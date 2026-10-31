using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    private static readonly SemaphoreSlim SyncLock = new(1, 1);

    public TmdbSyncService(
        ApplicationDbContext db,
        ITmdbService tmdbService,
        ICacheService cacheService,
        ILogger<TmdbSyncService> logger)
    {
        _db = db;
        _tmdbService = tmdbService;
        _cacheService = cacheService;
        _logger = logger;
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

    public async Task<TmdbSyncResultDto> RunFullSyncAsync(string triggeredBy = "Admin", CancellationToken cancellationToken = default)
    {
        if (!await SyncLock.WaitAsync(0, cancellationToken))
        {
            return new TmdbSyncResultDto(false, 0, 0, TimeSpan.Zero, "A TMDB synchronization is already in progress.");
        }

        var stopwatch = Stopwatch.StartNew();
        var syncLog = new TmdbSyncLog
        {
            StartedAt = DateTimeOffset.UtcNow,
            Status = "Running",
            TriggeredBy = triggeredBy
        };

        _db.TmdbSyncLogs.Add(syncLog);
        await _db.SaveChangesAsync(cancellationToken);

        var itemsSyncedCount = 0;
        var errorCount = 0;

        try
        {
            _logger.LogInformation("Starting TMDB catalog synchronization triggered by {TriggeredBy}...", triggeredBy);

            // 1. Sync genres
            await SyncGenresAsync(cancellationToken);

            // 2. Sync category lists
            var categoryEndpoints = new (string Endpoint, string Type, string Category)[]
            {
                ("trending/movie/week", "movie", "trending"),
                ("trending/tv/week", "series", "trending"),
                
                // Add more pages for popular movies (e.g. pages 1 to 5 = 100 titles)
                ("movie/popular?page=1", "movie", "popular"),
                ("movie/popular?page=2", "movie", "popular"),
                ("movie/popular?page=3", "movie", "popular"),
                ("movie/popular?page=4", "movie", "popular"),
                ("movie/popular?page=5", "movie", "popular"),
                // Add more pages for popular TV shows
                ("tv/popular?page=1", "series", "popular"),
                ("tv/popular?page=2", "series", "popular"),
                ("tv/popular?page=3", "series", "popular"),
                // Top rated movies & shows
                ("movie/top_rated?page=1", "movie", "top_rated"),
                ("movie/top_rated?page=2", "movie", "top_rated"),
                ("tv/top_rated?page=1", "series", "top_rated"),
                
                ("movie/upcoming?page=1", "movie", "upcoming"),
                ("movie/now_playing?page=1", "movie", "now_playing")
            };

            var processedTmdbIds = new HashSet<(int TmdbId, string Type)>();

            foreach (var (endpoint, type, category) in categoryEndpoints)
            {
                try
                {
                    var items = await _tmdbService.GetListEndpointAsync(endpoint, type, cancellationToken);
                    foreach (var item in items)
                    {
                        var key = (item.Id, type);
                        if (!processedTmdbIds.Contains(key))
                        {
                            processedTmdbIds.Add(key);
                        }

                        await UpsertTitleSummaryAsync(item, category, cancellationToken);
                        itemsSyncedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed syncing list endpoint {Endpoint}", endpoint);
                    errorCount++;
                }
            }

            // 3. For the top items (trending + popular), fetch full details (cast, trailers, seasons)
            var topTitlesToEnrich = await _db.Titles
                .Where(t => t.IsActive && t.TmdbListCategory != null && (t.TmdbListCategory.Contains("trending") || t.TmdbListCategory.Contains("popular")))
                .OrderByDescending(t => t.Popularity)
                .Take(40)
                .ToListAsync(cancellationToken);

            foreach (var title in topTitlesToEnrich)
            {
                try
                {
                    await EnrichTitleDetailsAsync(title, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to enrich details for title {TitleText} (TMDB ID {TmdbId})", title.TitleText, title.TmdbId);
                    errorCount++;
                }
            }

            // 4. Invalidate relevant caches
            await _cacheService.RemoveAsync("home_feed_v1", cancellationToken);

            stopwatch.Stop();
            syncLog.Status = "Completed";
            syncLog.CompletedAt = DateTimeOffset.UtcNow;
            syncLog.ItemsSynced = itemsSyncedCount;
            syncLog.Errors = errorCount;
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("TMDB catalog sync completed successfully in {Duration}s. Synced: {Synced}, Errors: {Errors}",
                stopwatch.Elapsed.TotalSeconds, itemsSyncedCount, errorCount);

            return new TmdbSyncResultDto(true, itemsSyncedCount, errorCount, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "TMDB catalog sync encountered a critical failure.");

            syncLog.Status = "Failed";
            syncLog.CompletedAt = DateTimeOffset.UtcNow;
            syncLog.ErrorMessage = ex.Message;
            syncLog.Errors = errorCount + 1;
            await _db.SaveChangesAsync(CancellationToken.None);

            return new TmdbSyncResultDto(false, itemsSyncedCount, errorCount + 1, stopwatch.Elapsed, ex.Message);
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
                    Type = detail.Type
                };
                _db.Titles.Add(existing);
            }

            ApplyFullDetailToEntity(existing, detail);
            await EnsureTitleSourceAsync(existing, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            await _cacheService.RemoveAsync("home_feed_v1", cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync single title TMDB {TmdbId}", tmdbId);
            return false;
        }
    }

    private async Task SyncGenresAsync(CancellationToken cancellationToken)
    {
        try
        {
            var genres = await _tmdbService.GetGenreMapAsync(cancellationToken);
            foreach (var (id, name) in genres)
            {
                var existing = await _db.Genres.FindAsync([id], cancellationToken);
                if (existing == null)
                {
                    _db.Genres.Add(new Genre
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
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync TMDB genres");
        }
    }

    private async Task UpsertTitleSummaryAsync(TmdbItemSummary summary, string categoryTag, CancellationToken cancellationToken)
    {
        var title = await _db.Titles
            .Include(t => t.TitleGenres)
            .Include(t => t.TitleSources)
            .FirstOrDefaultAsync(t => t.TmdbId == summary.Id, cancellationToken);

        var isNew = title == null;
        if (isNew)
        {
            title = new Title
            {
                TmdbId = summary.Id,
                Type = summary.Type
            };
            _db.Titles.Add(title);
        }

        title!.TitleText = summary.Title;
        title.OriginalTitle = summary.OriginalTitle ?? summary.Title;
        title.Overview = summary.Overview;
        title.PosterPath = summary.PosterPath;
        title.BackdropPath = summary.BackdropPath;
        title.TmdbRating = (decimal)Math.Round(summary.VoteAverage, 1);
        title.VoteCount = summary.VoteCount;
        title.Popularity = summary.Popularity;
        title.OriginalLanguage = summary.OriginalLanguage;
        title.IsActive = true;
        title.LastTmdbSyncAt = DateTimeOffset.UtcNow;
        title.UpdatedAt = DateTime.UtcNow;

        if (summary.Type == "movie" && DateTime.TryParse(summary.ReleaseDate, out var rd))
        {
            title.ReleaseDate = rd;
        }
        else if (summary.Type == "series" && DateTime.TryParse(summary.ReleaseDate, out var fd))
        {
            title.FirstAirDate = fd;
        }

        // Tag category
        var categories = string.IsNullOrEmpty(title.TmdbListCategory) 
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
            : new HashSet<string>(title.TmdbListCategory.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), StringComparer.OrdinalIgnoreCase);

        categories.Add(categoryTag);
        title.TmdbListCategory = string.Join(",", categories);

        // Associate genres
        if (summary.GenreIds.Count > 0)
        {
            foreach (var gid in summary.GenreIds)
            {
                if (!title.TitleGenres.Any(tg => tg.GenreId == gid))
                {
                    title.TitleGenres.Add(new TitleGenre { Title = title, GenreId = gid });
                }
            }
        }

        // Auto-generate source
        await EnsureTitleSourceAsync(title, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnrichTitleDetailsAsync(Title title, CancellationToken cancellationToken)
    {
        var detail = await _tmdbService.GetFullDetailsAsync(title.TmdbId, title.Type, cancellationToken);
        if (detail == null) return;

        ApplyFullDetailToEntity(title, detail);

        // Enrich Cast
        if (detail.Cast.Count > 0)
        {
            // Remove old cast
            var oldCasts = await _db.TitleCasts.Where(tc => tc.TitleId == title.Id).ToListAsync(cancellationToken);
            _db.TitleCasts.RemoveRange(oldCasts);

            foreach (var c in detail.Cast)
            {
                var person = await _db.People.FirstOrDefaultAsync(p => p.TmdbId == c.PersonTmdbId, cancellationToken);
                if (person == null)
                {
                    person = new Person
                    {
                        TmdbId = c.PersonTmdbId,
                        Name = c.Name,
                        ProfilePath = c.ProfilePath
                    };
                    _db.People.Add(person);
                    await _db.SaveChangesAsync(cancellationToken);
                }
                else if (string.IsNullOrEmpty(person.ProfilePath) && !string.IsNullOrEmpty(c.ProfilePath))
                {
                    person.ProfilePath = c.ProfilePath;
                }

                _db.TitleCasts.Add(new TitleCast
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
            var oldVideos = await _db.Videos.Where(v => v.TitleId == title.Id).ToListAsync(cancellationToken);
            _db.Videos.RemoveRange(oldVideos);

            foreach (var v in detail.Videos)
            {
                _db.Videos.Add(new Video
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
            var oldProviders = await _db.TitleProviders.Where(tp => tp.TitleId == title.Id).ToListAsync(cancellationToken);
            _db.TitleProviders.RemoveRange(oldProviders);

            foreach (var p in detail.Providers)
            {
                var provider = await _db.Providers.FirstOrDefaultAsync(pr => pr.TmdbId == p.ProviderTmdbId, cancellationToken);
                if (provider == null)
                {
                    provider = new Provider
                    {
                        TmdbId = p.ProviderTmdbId,
                        Name = p.Name,
                        LogoPath = p.LogoPath
                    };
                    _db.Providers.Add(provider);
                    await _db.SaveChangesAsync(cancellationToken);
                }

                _db.TitleProviders.Add(new TitleProvider
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
            var existingSeasons = await _db.Seasons.Where(s => s.TitleId == title.Id).ToListAsync(cancellationToken);

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
                    _db.Seasons.Add(season);
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

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyFullDetailToEntity(Title title, TmdbFullDetail detail)
    {
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

    private async Task EnsureTitleSourceAsync(Title title, CancellationToken cancellationToken)
    {
        if (title.TitleSources.Any(ts => ts.IsActive)) return;

        var vidsrcProvider = await _db.SourceProviders.FirstOrDefaultAsync(sp => sp.Slug == AppConstants.Providers.VidSrc, cancellationToken);
        if (vidsrcProvider == null) return;

        var embedUrl = title.Type == "series"
            ? $"https://vidsrc.to/embed/tv/{title.TmdbId}/1/1"
            : $"https://vidsrc.to/embed/movie/{title.TmdbId}";

        title.TitleSources.Add(new TitleSource
        {
            Title = title,
            SourceProviderId = vidsrcProvider.Id,
            EmbedUrl = embedUrl,
            Quality = "1080p",
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
