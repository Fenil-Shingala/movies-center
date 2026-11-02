using System.Diagnostics;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.Admin.Dtos;
using MoviesCenter.Domain.Constants;
using MoviesCenter.Domain.Entities;
using MoviesCenter.Infrastructure.Data;

namespace MoviesCenter.Infrastructure.Services;

public class TmdbSyncService : ITmdbSyncService
{
    private readonly ApplicationDbContext _db;
    private readonly ITmdbService _tmdbService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<TmdbSyncService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private static readonly SemaphoreSlim SyncLock = new(1, 1);

    public TmdbSyncService(
        ApplicationDbContext db,
        ITmdbService tmdbService,
        ICacheService cacheService,
        ILogger<TmdbSyncService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _db = db;
        _tmdbService = tmdbService;
        _cacheService = cacheService;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task<TmdbSyncStatusDto> GetSyncStatusAsync(CancellationToken cancellationToken = default)
    {
        var latestLog = await _db.TmdbSyncLogs
            .OrderByDescending(l => l.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var isRunning = SyncLock.CurrentCount == 0 || (latestLog != null && latestLog.Status == "Running");

        return new TmdbSyncStatusDto(
            IsRunning: isRunning,
            LastRunAt: latestLog?.StartedAt,
            LastStatus: latestLog?.Status,
            LastItemsSynced: latestLog?.ItemsSynced ?? 0,
            LastErrors: latestLog?.Errors ?? 0,
            TriggeredBy: latestLog?.TriggeredBy,
            ErrorMessage: latestLog?.ErrorMessage
        );
    }

    public Task<TmdbSyncResultDto> RunFullSyncAsync(string triggeredBy = "Admin", int? limit = null, CancellationToken cancellationToken = default)
    {
        if (SyncLock.CurrentCount == 0)
        {
            return Task.FromResult(new TmdbSyncResultDto(false, 0, 0, TimeSpan.Zero, "A TMDB synchronization is already in progress.", "A TMDB synchronization is already in progress."));
        }

        // Launch in background so HTTP request returns immediately and does not time out
        _ = Task.Run(async () =>
        {
            await ExecuteFullSyncBackgroundAsync(triggeredBy, limit);
        }, CancellationToken.None);

        var limitMsg = limit.HasValue && limit.Value > 0
            ? $"TMDB synchronization initiated for up to {limit.Value:N0} new/updated titles (duplicates skipped)."
            : "Extensive TMDB catalog synchronization initiated in background (duplicates skipped).";

        return Task.FromResult(new TmdbSyncResultDto(true, 0, 0, TimeSpan.Zero, null, limitMsg));
    }

    private async Task ExecuteFullSyncBackgroundAsync(string triggeredBy, int? limit = null)
    {
        if (!await SyncLock.WaitAsync(0))
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tmdbService = scope.ServiceProvider.GetRequiredService<ITmdbService>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var stopwatch = Stopwatch.StartNew();
        var syncLog = new TmdbSyncLog
        {
            StartedAt = DateTimeOffset.UtcNow,
            Status = "Running",
            TriggeredBy = limit.HasValue && limit.Value > 0 ? $"{triggeredBy} (Limit: {limit.Value})" : triggeredBy
        };

        db.TmdbSyncLogs.Add(syncLog);
        await db.SaveChangesAsync();

        var itemsSyncedCount = 0;
        var duplicatesSkippedCount = 0;
        var errorCount = 0;

        try
        {
            _logger.LogInformation("Starting TMDB catalog synchronization triggered by {TriggeredBy} with limit {Limit}...", 
                triggeredBy, limit?.ToString() ?? "Unlimited");

            // Pre-fetch the VidLink/VidSrc provider once to avoid querying it repeatedly in loops
            var vidsrcProviderId = await db.SourceProviders
                .Where(sp => sp.Slug == AppConstants.Providers.VidSrc)
                .Select(sp => (int?)sp.Id)
                .FirstOrDefaultAsync();

            // 1. Sync genres
            await SyncGenresAsync(db, tmdbService, CancellationToken.None);

            // 2. Generate comprehensive list of category and discover endpoints
            var categoryEndpoints = GenerateSyncEndpoints();
            var processedTmdbIds = new HashSet<(int TmdbId, string Type)>();

            foreach (var (endpoint, type, category) in categoryEndpoints)
            {
                if (limit.HasValue && limit.Value > 0 && itemsSyncedCount >= limit.Value)
                {
                    _logger.LogInformation("Target sync limit of {Limit} items reached. Halting catalog scan.", limit.Value);
                    break;
                }

                try
                {
                    List<TmdbItemSummary>? items = null;

                    // Retry up to 3 times with polite backoff if rate limited (429)
                    for (int attempt = 1; attempt <= 3; attempt++)
                    {
                        try
                        {
                            items = await tmdbService.GetListEndpointAsync(endpoint, type, CancellationToken.None);
                            break;
                        }
                        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            _logger.LogWarning("TMDB rate limit hit on {Endpoint}. Retrying in 2.5s (attempt {Attempt}/3)...", endpoint, attempt);
                            await Task.Delay(2500);
                        }
                    }

                    if (items == null || items.Count == 0) continue;

                    // Micro-pause between TMDB requests (polite rate limiting)
                    await Task.Delay(35);

                    // Batch load existing titles from DB for this page of items (1 single query instead of 20)
                    var batchTmdbIds = items.Select(i => i.Id).Distinct().ToList();
                    var existingTitlesMap = await db.Titles
                        .Include(t => t.TitleGenres)
                        .Include(t => t.TitleSources)
                        .Where(t => batchTmdbIds.Contains(t.TmdbId))
                        .ToDictionaryAsync(t => t.TmdbId);

                    var newOrUpdatedInBatch = 0;
                    foreach (var item in items)
                    {
                        if (limit.HasValue && limit.Value > 0 && (itemsSyncedCount + newOrUpdatedInBatch) >= limit.Value)
                        {
                            break;
                        }

                        var key = (item.Id, type);
                        if (!processedTmdbIds.Contains(key))
                        {
                            processedTmdbIds.Add(key);
                            existingTitlesMap.TryGetValue(item.Id, out var existingTitle);
                            var changed = UpsertTitleSummary(db, existingTitle, item, category, vidsrcProviderId);

                            if (changed)
                            {
                                newOrUpdatedInBatch++;
                            }
                            else
                            {
                                duplicatesSkippedCount++;
                            }
                        }
                    }

                    if (newOrUpdatedInBatch > 0)
                    {
                        itemsSyncedCount += newOrUpdatedInBatch;

                        // Periodically commit live itemsSyncedCount to TmdbSyncLog so live progress is visible in UI
                        syncLog.ItemsSynced = itemsSyncedCount;
                        await db.SaveChangesAsync();
                    }

                    if (limit.HasValue && limit.Value > 0 && itemsSyncedCount >= limit.Value)
                    {
                        _logger.LogInformation("Reached sync limit of {Limit} items.", limit.Value);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed syncing list endpoint {Endpoint}", endpoint);
                    errorCount++;
                }
            }

            // 3. Enrich details (cast, trailers, seasons breakdown)
            // CRITICAL: DO NOT fetch duplicate details for titles that already have them in DB!
            // Only fetch details from TMDB API for titles that lack cast, trailers, or seasons.
            var enrichLimit = limit.HasValue && limit.Value > 0 ? Math.Min(limit.Value, 50) : 50;

            var titlesToEnrich = await db.Titles
                .Where(t => t.IsActive && (!t.TitleCasts.Any() || !t.Videos.Any() || (t.Type == "series" && !t.Seasons.Any())))
                .OrderByDescending(t => t.Popularity)
                .Take(enrichLimit)
                .ToListAsync();

            if (titlesToEnrich.Count > 0)
            {
                _logger.LogInformation("Enriching {Count} titles missing full details from TMDB API...", titlesToEnrich.Count);

                foreach (var title in titlesToEnrich)
                {
                    try
                    {
                        await EnrichTitleDetailsAsync(db, tmdbService, title, CancellationToken.None);
                        await Task.Delay(35);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to enrich details for title {TitleText} (TMDB ID {TmdbId})", title.TitleText, title.TmdbId);
                        errorCount++;
                    }
                }
            }

            // 4. Invalidate home feed cache so new catalog immediately reflects
            await cacheService.RemoveAsync("home_feed_v1");

            stopwatch.Stop();
            syncLog.Status = "Completed";
            syncLog.CompletedAt = DateTimeOffset.UtcNow;
            syncLog.ItemsSynced = itemsSyncedCount;
            syncLog.Errors = errorCount;
            syncLog.ErrorMessage = duplicatesSkippedCount > 0
                ? $"{duplicatesSkippedCount:N0} duplicates skipped (already up-to-date in DB)"
                : null;
            await db.SaveChangesAsync();

            _logger.LogInformation("TMDB catalog sync completed successfully in {Duration:F1}s. Synced: {Synced}, Duplicates skipped: {Duplicates}, Errors: {Errors}",
                stopwatch.Elapsed.TotalSeconds, itemsSyncedCount, duplicatesSkippedCount, errorCount);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "TMDB catalog sync encountered a critical failure.");

            syncLog.Status = "Failed";
            syncLog.CompletedAt = DateTimeOffset.UtcNow;
            syncLog.ErrorMessage = ex.Message;
            syncLog.Errors = errorCount + 1;
            await db.SaveChangesAsync();
        }
        finally
        {
            SyncLock.Release();
        }
    }

    public async Task<bool> SyncSingleTitleAsync(int tmdbId, string type, CancellationToken cancellationToken = default)
    {
        try
        {
            var detail = await _tmdbService.GetFullDetailsAsync(tmdbId, type, cancellationToken);
            if (detail == null) return false;

            var existing = await _db.Titles
                .Include(t => t.TitleGenres)
                .Include(t => t.TitleSources)
                .FirstOrDefaultAsync(t => t.TmdbId == tmdbId, cancellationToken);

            if (existing == null)
            {
                existing = new Title
                {
                    TmdbId = detail.TmdbId,
                    Type = detail.Type,
                    TitleText = detail.Title,
                    OriginalTitle = detail.OriginalTitle ?? detail.Title,
                    OriginalLanguage = detail.OriginalLanguage ?? "en",
                    IsActive = true
                };
                _db.Titles.Add(existing);
            }

            ApplyFullDetailToEntity(existing, detail);
            await EnsureTitleSourceAsync(_db, existing, cancellationToken);

            // Associate Genres if available
            if (detail.GenreIds.Count > 0)
            {
                var existingGenreIds = await _db.Genres
                    .Where(g => detail.GenreIds.Contains(g.Id))
                    .Select(g => g.Id)
                    .ToListAsync(cancellationToken);

                foreach (var gid in detail.GenreIds)
                {
                    if (existingGenreIds.Contains(gid) && !existing.TitleGenres.Any(tg => tg.GenreId == gid))
                    {
                        existing.TitleGenres.Add(new TitleGenre { Title = existing, GenreId = gid });
                    }
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            // Enrich Cast, Videos (trailers), Providers, Seasons
            await EnrichRelatedEntitiesAsync(_db, existing, detail, cancellationToken);

            // Repair any titles that might have been previously saved with blank TitleText
            await RepairBlankTitlesAsync(_db, _tmdbService, cancellationToken);

            await _cacheService.RemoveAsync("home_feed_v1", cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync single title TMDB {TmdbId}", tmdbId);
            return false;
        }
    }

    private static List<(string Endpoint, string Type, string Category)> GenerateSyncEndpoints()
    {
        var endpoints = new List<(string Endpoint, string Type, string Category)>
        {
            // 1. Trending Day & Week
            ("trending/movie/day", "movie", "trending"),
            ("trending/movie/week", "movie", "trending"),
            ("trending/tv/day", "series", "trending"),
            ("trending/tv/week", "series", "trending")
        };

        // 2. Popular Movies (150 pages = 3,000 titles)
        for (int p = 1; p <= 150; p++)
        {
            endpoints.Add(($"movie/popular?page={p}", "movie", "popular"));
        }

        // 3. Popular TV Series (100 pages = 2,000 titles)
        for (int p = 1; p <= 100; p++)
        {
            endpoints.Add(($"tv/popular?page={p}", "series", "popular"));
        }

        // 4. Top Rated Movies (100 pages = 2,000 titles)
        for (int p = 1; p <= 100; p++)
        {
            endpoints.Add(($"movie/top_rated?page={p}", "movie", "top_rated"));
        }

        // 5. Top Rated TV Series (80 pages = 1,600 titles)
        for (int p = 1; p <= 80; p++)
        {
            endpoints.Add(($"tv/top_rated?page={p}", "series", "top_rated"));
        }

        // 6. Theatrical & Broadcast (Movie Now Playing, Upcoming, TV On The Air, Airing Today)
        for (int p = 1; p <= 20; p++)
        {
            endpoints.Add(($"movie/now_playing?page={p}", "movie", "now_playing"));
            endpoints.Add(($"movie/upcoming?page={p}", "movie", "upcoming"));
            endpoints.Add(($"tv/on_the_air?page={p}", "series", "on_the_air"));
        }
        for (int p = 1; p <= 10; p++)
        {
            endpoints.Add(($"tv/airing_today?page={p}", "series", "airing_today"));
        }

        // 7. Modern Era by Year (2016 through 2026 - 11 years)
        // 8 pages movies (160) + 5 pages TV (100) per year = 2,860 titles
        for (int year = 2026; year >= 2016; year--)
        {
            for (int p = 1; p <= 8; p++)
            {
                endpoints.Add(($"discover/movie?primary_release_year={year}&sort_by=popularity.desc&vote_count.gte=15&page={p}", "movie", $"year_{year}"));
            }
            for (int p = 1; p <= 5; p++)
            {
                endpoints.Add(($"discover/tv?first_air_date_year={year}&sort_by=popularity.desc&vote_count.gte=10&page={p}", "series", $"year_{year}"));
            }
        }

        // 8. 2000s & Early 2010s Blockbusters (2000 to 2015 - 16 years)
        // 5 pages movies (100) + 3 pages TV (60) per year = 2,560 titles
        for (int year = 2015; year >= 2000; year--)
        {
            for (int p = 1; p <= 5; p++)
            {
                endpoints.Add(($"discover/movie?primary_release_year={year}&sort_by=vote_count.desc&vote_count.gte=50&page={p}", "movie", "classics"));
            }
            for (int p = 1; p <= 3; p++)
            {
                endpoints.Add(($"discover/tv?first_air_date_year={year}&sort_by=vote_count.desc&vote_count.gte=20&page={p}", "series", "classics"));
            }
        }

        // 9. Golden Era Classics (1980 to 1999 - 20 years)
        // 4 pages movies (80) + 2 pages TV (40) per year = 2,400 titles
        for (int year = 1999; year >= 1980; year--)
        {
            for (int p = 1; p <= 4; p++)
            {
                endpoints.Add(($"discover/movie?primary_release_year={year}&sort_by=vote_count.desc&vote_count.gte=75&page={p}", "movie", "golden_era"));
            }
            for (int p = 1; p <= 2; p++)
            {
                endpoints.Add(($"discover/tv?first_air_date_year={year}&sort_by=vote_count.desc&vote_count.gte=20&page={p}", "series", "golden_era"));
            }
        }

        // 10. Discover by All 19 TMDB Movie Genres (19 genres * 4 pages = 1,520 titles)
        var movieGenres = new[] { 28, 12, 16, 35, 80, 99, 18, 10751, 14, 36, 27, 10402, 9648, 10749, 878, 10770, 53, 10752, 37 };
        foreach (var genreId in movieGenres)
        {
            for (int p = 1; p <= 4; p++)
            {
                endpoints.Add(($"discover/movie?with_genres={genreId}&sort_by=vote_average.desc&vote_count.gte=100&page={p}", "movie", "genre_highlight"));
            }
        }

        // 11. Discover by Major TV Genres (10 genres * 4 pages = 800 titles)
        var tvGenres = new[] { 10759, 16, 35, 80, 18, 10751, 9648, 10765, 10768, 37 };
        foreach (var genreId in tvGenres)
        {
            for (int p = 1; p <= 4; p++)
            {
                endpoints.Add(($"discover/tv?with_genres={genreId}&sort_by=vote_average.desc&vote_count.gte=50&page={p}", "series", "genre_highlight"));
            }
        }

        return endpoints;
    }

    private static async Task SyncGenresAsync(ApplicationDbContext db, ITmdbService tmdbService, CancellationToken cancellationToken)
    {
        try
        {
            var genres = await tmdbService.GetGenreMapAsync(cancellationToken);
            foreach (var (id, name) in genres)
            {
                var existing = await db.Genres.FindAsync([id], cancellationToken);
                if (existing == null)
                {
                    db.Genres.Add(new Genre
                    {
                        Id = id,
                        Name = name,
                        Slug = GenerateSlug(name)
                    });
                }
                else
                {
                    existing.Name = name;
                }
            }
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Log non-fatal genre sync error
            Debug.WriteLine($"Failed to sync TMDB genres: {ex.Message}");
        }
    }

    private static bool UpsertTitleSummary(
        ApplicationDbContext db, 
        Title? title, 
        TmdbItemSummary summary, 
        string categoryTag, 
        int? vidsrcProviderId)
    {
        var isNew = title == null;
        if (isNew)
        {
            title = new Title
            {
                TmdbId = summary.Id,
                Type = summary.Type,
                TitleText = summary.Title,
                OriginalTitle = summary.OriginalTitle ?? summary.Title,
                Overview = summary.Overview,
                PosterPath = summary.PosterPath,
                BackdropPath = summary.BackdropPath,
                TmdbRating = (decimal)Math.Round(summary.VoteAverage, 1),
                VoteCount = summary.VoteCount,
                Popularity = summary.Popularity,
                OriginalLanguage = summary.OriginalLanguage ?? "en",
                IsActive = true,
                LastTmdbSyncAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TmdbListCategory = categoryTag
            };

            if (summary.Type == "movie" && DateTime.TryParse(summary.ReleaseDate, out var rd))
            {
                title.ReleaseDate = rd;
            }
            else if (summary.Type == "series" && DateTime.TryParse(summary.ReleaseDate, out var fd))
            {
                title.FirstAirDate = fd;
            }

            // Associate genres
            if (summary.GenreIds.Count > 0)
            {
                foreach (var gid in summary.GenreIds)
                {
                    title.TitleGenres.Add(new TitleGenre { Title = title, GenreId = gid });
                }
            }

            // Auto-generate source (VidLink HD primary)
            EnsureTitleSource(title, vidsrcProviderId);

            db.Titles.Add(title);
            return true;
        }

        // Title already exists in DB! Check if data actually changed or is duplicate
        var hasChanged = false;

        if (string.IsNullOrWhiteSpace(title!.TitleText) || title.TitleText != summary.Title)
        {
            title.TitleText = summary.Title;
            hasChanged = true;
        }

        if (title.OriginalTitle == null && summary.OriginalTitle != null)
        {
            title.OriginalTitle = summary.OriginalTitle;
            hasChanged = true;
        }

        if (string.IsNullOrWhiteSpace(title.Overview) && !string.IsNullOrWhiteSpace(summary.Overview))
        {
            title.Overview = summary.Overview;
            hasChanged = true;
        }

        if (title.PosterPath != summary.PosterPath && !string.IsNullOrEmpty(summary.PosterPath))
        {
            title.PosterPath = summary.PosterPath;
            hasChanged = true;
        }

        if (title.BackdropPath != summary.BackdropPath && !string.IsNullOrEmpty(summary.BackdropPath))
        {
            title.BackdropPath = summary.BackdropPath;
            hasChanged = true;
        }

        var newRating = (decimal)Math.Round(summary.VoteAverage, 1);
        if (title.TmdbRating != newRating)
        {
            title.TmdbRating = newRating;
            hasChanged = true;
        }

        if (title.VoteCount != summary.VoteCount)
        {
            title.VoteCount = summary.VoteCount;
            hasChanged = true;
        }

        if (Math.Abs(title.Popularity - summary.Popularity) > 0.5)
        {
            title.Popularity = summary.Popularity;
            hasChanged = true;
        }

        // Tag category if not already present
        var categories = string.IsNullOrEmpty(title.TmdbListCategory) 
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
            : new HashSet<string>(title.TmdbListCategory.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), StringComparer.OrdinalIgnoreCase);

        if (!categories.Contains(categoryTag))
        {
            categories.Add(categoryTag);
            title.TmdbListCategory = string.Join(",", categories);
            hasChanged = true;
        }

        // Associate missing genres
        if (summary.GenreIds.Count > 0)
        {
            foreach (var gid in summary.GenreIds)
            {
                if (!title.TitleGenres.Any(tg => tg.GenreId == gid))
                {
                    title.TitleGenres.Add(new TitleGenre { Title = title, GenreId = gid });
                    hasChanged = true;
                }
            }
        }

        // Auto-generate source (VidLink HD primary) if missing
        if (!title.TitleSources.Any(ts => ts.IsActive))
        {
            EnsureTitleSource(title, vidsrcProviderId);
            hasChanged = true;
        }

        if (hasChanged)
        {
            title.LastTmdbSyncAt = DateTimeOffset.UtcNow;
            title.UpdatedAt = DateTime.UtcNow;
            return true;
        }

        // Title is identical to DB record - duplicate skipped!
        return false;
    }

    private static void EnsureTitleSource(Title title, int? vidsrcProviderId)
    {
        if (title.TitleSources.Any(ts => ts.IsActive)) return;
        if (!vidsrcProviderId.HasValue) return;

        var embedUrl = title.Type == "series"
            ? $"https://vidlink.pro/tv/{title.TmdbId}/1/1"
            : $"https://vidlink.pro/movie/{title.TmdbId}";

        title.TitleSources.Add(new TitleSource
        {
            Title = title,
            SourceProviderId = vidsrcProviderId.Value,
            EmbedUrl = embedUrl,
            Quality = "1080p HD",
            IsActive = true,
            SeasonNumber = title.Type == "series" ? 1 : null,
            EpisodeNumber = title.Type == "series" ? 1 : null
        });
    }

    private static async Task EnrichTitleDetailsAsync(ApplicationDbContext db, ITmdbService tmdbService, Title title, CancellationToken cancellationToken)
    {
        var detail = await tmdbService.GetFullDetailsAsync(title.TmdbId, title.Type, cancellationToken);
        if (detail == null) return;

        ApplyFullDetailToEntity(title, detail);
        await EnrichRelatedEntitiesAsync(db, title, detail, cancellationToken);
    }

    private static async Task EnrichRelatedEntitiesAsync(ApplicationDbContext db, Title title, TmdbFullDetail detail, CancellationToken cancellationToken)
    {
        // Enrich Cast
        if (detail.Cast.Count > 0)
        {
            var oldCasts = await db.TitleCasts.Where(tc => tc.TitleId == title.Id).ToListAsync(cancellationToken);
            db.TitleCasts.RemoveRange(oldCasts);

            foreach (var c in detail.Cast)
            {
                var person = await db.People.FirstOrDefaultAsync(p => p.TmdbId == c.PersonTmdbId, cancellationToken);
                if (person == null)
                {
                    person = new Person
                    {
                        TmdbId = c.PersonTmdbId,
                        Name = c.Name,
                        ProfilePath = c.ProfilePath
                    };
                    db.People.Add(person);
                    await db.SaveChangesAsync(cancellationToken);
                }
                else if (string.IsNullOrEmpty(person.ProfilePath) && !string.IsNullOrEmpty(c.ProfilePath))
                {
                    person.ProfilePath = c.ProfilePath;
                }

                db.TitleCasts.Add(new TitleCast
                {
                    TitleId = title.Id,
                    PersonId = person.Id,
                    Character = c.Character,
                    DisplayOrder = c.DisplayOrder
                });
            }
        }

        // Enrich Videos (trailers)
        if (detail.Videos.Count > 0)
        {
            var oldVideos = await db.Videos.Where(v => v.TitleId == title.Id).ToListAsync(cancellationToken);
            db.Videos.RemoveRange(oldVideos);

            foreach (var v in detail.Videos)
            {
                db.Videos.Add(new Video
                {
                    TitleId = title.Id,
                    TmdbKey = v.Key,
                    Name = v.Name,
                    Type = v.Type,
                    Site = v.Site
                });
            }
        }

        // Enrich Providers
        if (detail.Providers.Count > 0)
        {
            var oldProviders = await db.TitleProviders.Where(tp => tp.TitleId == title.Id).ToListAsync(cancellationToken);
            db.TitleProviders.RemoveRange(oldProviders);

            foreach (var p in detail.Providers)
            {
                var provider = await db.Providers.FirstOrDefaultAsync(pr => pr.TmdbId == p.ProviderTmdbId, cancellationToken);
                if (provider == null)
                {
                    provider = new Provider
                    {
                        TmdbId = p.ProviderTmdbId,
                        Name = p.Name,
                        LogoPath = p.LogoPath
                    };
                    db.Providers.Add(provider);
                    await db.SaveChangesAsync(cancellationToken);
                }

                db.TitleProviders.Add(new TitleProvider
                {
                    TitleId = title.Id,
                    ProviderId = provider.Id,
                    Type = p.Type,
                    Country = p.Country
                });
            }
        }

        // If Series, enrich Seasons
        if (title.Type == "series" && detail.Seasons.Count > 0)
        {
            var existingSeasons = await db.Seasons.Where(s => s.TitleId == title.Id).ToListAsync(cancellationToken);

            foreach (var s in detail.Seasons)
            {
                var season = existingSeasons.FirstOrDefault(x => x.SeasonNumber == s.SeasonNumber);
                if (season == null)
                {
                    season = new Season
                    {
                        TitleId = title.Id,
                        TmdbId = s.TmdbId,
                        SeasonNumber = s.SeasonNumber,
                        Name = s.Name,
                        Overview = s.Overview,
                        PosterPath = s.PosterPath,
                        EpisodeCount = s.EpisodeCount
                    };
                    if (DateTime.TryParse(s.AirDate, out var sa)) season.AirDate = sa;
                    db.Seasons.Add(season);
                }
                else
                {
                    season.Name = s.Name;
                    season.Overview = s.Overview;
                    season.PosterPath = s.PosterPath;
                    season.EpisodeCount = s.EpisodeCount;
                    if (DateTime.TryParse(s.AirDate, out var sa)) season.AirDate = sa;
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task RepairBlankTitlesAsync(ApplicationDbContext db, ITmdbService tmdbService, CancellationToken cancellationToken)
    {
        try
        {
            var blankTitles = await db.Titles
                .Include(t => t.TitleSources)
                .Where(t => string.IsNullOrWhiteSpace(t.TitleText) || t.TitleText == "" || t.OriginalTitle == null)
                .Take(25)
                .ToListAsync(cancellationToken);

            foreach (var t in blankTitles)
            {
                var detail = await tmdbService.GetFullDetailsAsync(t.TmdbId, t.Type, cancellationToken);
                if (detail != null)
                {
                    ApplyFullDetailToEntity(t, detail);
                    await EnsureTitleSourceAsync(db, t, cancellationToken);
                    await EnrichRelatedEntitiesAsync(db, t, detail, cancellationToken);
                }
            }

            if (blankTitles.Count > 0)
            {
                await db.SaveChangesAsync(cancellationToken);
            }
        }
        catch
        {
            // Non-fatal repair failure
        }
    }

    private static void ApplyFullDetailToEntity(Title title, TmdbFullDetail detail)
    {
        if (!string.IsNullOrWhiteSpace(detail.Title))
        {
            title.TitleText = detail.Title;
        }
        else if (string.IsNullOrWhiteSpace(title.TitleText))
        {
            title.TitleText = "Untitled";
        }

        title.OriginalTitle = !string.IsNullOrWhiteSpace(detail.OriginalTitle) ? detail.OriginalTitle : (detail.Title ?? title.OriginalTitle);
        title.OriginalLanguage = !string.IsNullOrWhiteSpace(detail.OriginalLanguage) ? detail.OriginalLanguage : (string.IsNullOrWhiteSpace(title.OriginalLanguage) ? "en" : title.OriginalLanguage);
        title.IsActive = true;
        title.Tagline = detail.Tagline;
        title.Overview = detail.Overview ?? title.Overview;
        title.PosterPath = detail.PosterPath ?? title.PosterPath;
        title.BackdropPath = detail.BackdropPath ?? title.BackdropPath;
        title.RuntimeMinutes = detail.RuntimeMinutes ?? title.RuntimeMinutes;
        title.TotalSeasons = detail.TotalSeasons ?? title.TotalSeasons;
        title.TotalEpisodes = detail.TotalEpisodes ?? title.TotalEpisodes;
        title.Status = detail.Status;
        title.VoteCount = detail.VoteCount > 0 ? detail.VoteCount : title.VoteCount;
        title.Popularity = detail.Popularity > 0 ? detail.Popularity : title.Popularity;
        title.TmdbRating = (decimal)Math.Round(detail.VoteAverage, 1);
        title.LastTmdbSyncAt = DateTimeOffset.UtcNow;
        title.UpdatedAt = DateTime.UtcNow;

        if (detail.Type == "movie" && DateTime.TryParse(detail.ReleaseDate, out var rd))
        {
            title.ReleaseDate = rd;
        }
        else if (detail.Type == "series" && DateTime.TryParse(detail.FirstAirDate, out var fd))
        {
            title.FirstAirDate = fd;
        }
    }

    private static async Task EnsureTitleSourceAsync(ApplicationDbContext db, Title title, CancellationToken cancellationToken)
    {
        if (title.TitleSources.Any(ts => ts.IsActive)) return;

        var vidsrcProvider = await db.SourceProviders.FirstOrDefaultAsync(sp => sp.Slug == AppConstants.Providers.VidSrc, cancellationToken);
        if (vidsrcProvider == null) return;

        var embedUrl = title.Type == "series"
            ? $"https://vidlink.pro/tv/{title.TmdbId}/1/1"
            : $"https://vidlink.pro/movie/{title.TmdbId}";

        title.TitleSources.Add(new TitleSource
        {
            Title = title,
            SourceProviderId = vidsrcProvider.Id,
            EmbedUrl = embedUrl,
            Quality = "1080p HD",
            IsActive = true,
            SeasonNumber = title.Type == "series" ? 1 : null,
            EpisodeNumber = title.Type == "series" ? 1 : null
        });
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("&", "and")
            .Replace("/", "-");
    }
}
