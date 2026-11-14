using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.Admin.Dtos;
using MoviesCenter.Application.Features.Catalog.Dtos;
using MoviesCenter.Application.Features.Catalog.Queries;
using MoviesCenter.Application.Features.Home.Dtos;
using MoviesCenter.Application.Features.Player.Dtos;
using MoviesCenter.Application.Features.Search.Dtos;
using MoviesCenter.Application.Features.Search.Queries;
using MoviesCenter.Domain.Entities;
using MoviesCenter.Infrastructure.Data;

namespace MoviesCenter.Infrastructure.Services;

public class CatalogQueryService : ICatalogQueryService
{
    private readonly ApplicationDbContext _db;
    private readonly ICacheService _cacheService;
    private readonly ITmdbService _tmdbService;
    private readonly ITmdbSyncService _tmdbSyncService;
    private readonly ILogger<CatalogQueryService> _logger;
    private const string HomeFeedCacheKey = "home_feed_v1";
    private static readonly TimeSpan HomeFeedCacheDuration = TimeSpan.FromHours(1);
    private const string GenresCacheKey = "genres_all_v1";
    private static readonly TimeSpan GenresCacheDuration = TimeSpan.FromDays(1);

    public CatalogQueryService(
        ApplicationDbContext db, 
        ICacheService cacheService,
        ITmdbService tmdbService,
        ITmdbSyncService tmdbSyncService,
        ILogger<CatalogQueryService> logger)
    {
        _db = db;
        _cacheService = cacheService;
        _tmdbService = tmdbService;
        _tmdbSyncService = tmdbSyncService;
        _logger = logger;
    }

    public async Task<HomeFeedDto> GetHomeFeedAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cacheService.GetAsync<HomeFeedDto>(HomeFeedCacheKey, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        // Query active titles from SQL Server
        var trendingMovies = await GetTitlesInternalAsync("trending", "movie", 20, cancellationToken);
        var trendingSeries = await GetTitlesInternalAsync("trending", "series", 20, cancellationToken);
        var popularMovies = await GetTitlesInternalAsync("popular", "movie", 20, cancellationToken);
        var topRatedMovies = await GetTitlesInternalAsync("top_rated", "movie", 20, cancellationToken);
        var popularSeries = await GetTitlesInternalAsync("popular", "series", 20, cancellationToken);
        var upcomingMovies = await GetTitlesInternalAsync("upcoming", "movie", 20, cancellationToken);

        // Featured Hero Candidate (first trending movie with backdrop, or fallback to any trending)
        var heroCandidate = trendingMovies.FirstOrDefault(m => !string.IsNullOrEmpty(m.BackdropPath)) 
            ?? trendingMovies.FirstOrDefault() 
            ?? popularMovies.FirstOrDefault()
            ?? new MediaCardDto(0, "movie", "Featured Title", null, null, 8.5, DateTime.UtcNow.Year.ToString(), ["Action", "Adventure"]);

        // Detailed hero info from SQL
        var heroTitle = await _db.Titles
            .Include(t => t.TitleGenres)
                .ThenInclude(tg => tg.Genre)
            .FirstOrDefaultAsync(t => t.TmdbId == heroCandidate.TmdbId, cancellationToken);

        var hero = heroTitle != null
            ? new HeroItemDto(
                TmdbId: heroTitle.TmdbId,
                Type: heroTitle.Type,
                Title: heroTitle.TitleText,
                Overview: !string.IsNullOrWhiteSpace(heroTitle.Overview) 
                    ? heroTitle.Overview 
                    : "Experience high-definition streaming with fast playback and zero interruptions.",
                BackdropPath: heroTitle.BackdropPath,
                PosterPath: heroTitle.PosterPath,
                Rating: (double)(heroTitle.TmdbRating ?? 8.0m),
                ReleaseDate: heroTitle.ReleaseDate?.ToString("yyyy-MM-dd") ?? heroTitle.FirstAirDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                Year: heroTitle.ReleaseDate?.Year.ToString() ?? heroTitle.FirstAirDate?.Year.ToString() ?? DateTime.UtcNow.Year.ToString(),
                RuntimeMinutes: heroTitle.RuntimeMinutes,
                TotalSeasons: heroTitle.TotalSeasons,
                Genres: heroTitle.TitleGenres.Select(tg => tg.Genre.Name).ToList()
            )
            : new HeroItemDto(
                TmdbId: heroCandidate.TmdbId,
                Type: heroCandidate.Type,
                Title: heroCandidate.Title,
                Overview: "Experience high-definition streaming with fast playback and zero interruptions.",
                BackdropPath: heroCandidate.BackdropPath,
                PosterPath: heroCandidate.PosterPath,
                Rating: heroCandidate.Rating,
                ReleaseDate: DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Year: heroCandidate.Year,
                RuntimeMinutes: 120,
                TotalSeasons: null,
                Genres: heroCandidate.Genres
            );

        var rows = new List<ContentRowDto>
        {
            new("trending-movies", "Trending Movies", "movie", "/movies?sort=popularity.desc", trendingMovies),
            new("trending-series", "Trending Series", "series", "/series?sort=popularity.desc", trendingSeries),
            new("popular-movies", "Popular Movies", "movie", "/movies?sort=popularity.desc", popularMovies),
            new("top-rated-movies", "Top Rated Movies", "movie", "/movies?sort=rating.desc", topRatedMovies),
            new("popular-series", "Popular Series", "series", "/series?sort=popularity.desc", popularSeries),
            new("upcoming-movies", "Upcoming Movies", "movie", "/movies?sort=release_date.desc", upcomingMovies)
        };

        var top10 = await GetTop10Async("all", cancellationToken);
        var feed = new HomeFeedDto(hero, rows, top10);

        // Cache in Redis
        await _cacheService.SetAsync(HomeFeedCacheKey, feed, HomeFeedCacheDuration, cancellationToken);

        return feed;
    }

    public async Task<List<MediaCardDto>> GetTitlesByCategoryAsync(string category, string type, int limit = 20, CancellationToken cancellationToken = default)
    {
        return await GetTitlesInternalAsync(category, type, limit, cancellationToken);
    }

    public async Task<HeroItemDto?> GetFeaturedHeroAsync(CancellationToken cancellationToken = default)
    {
        var feed = await GetHomeFeedAsync(cancellationToken);
        return feed.Hero;
    }

    public async Task<PagedResult<MediaCardDto>> BrowseTitlesAsync(BrowseTitlesQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : (query.PageSize > 100 ? 100 : query.PageSize);
        var type = string.Equals(query.Type, "series", StringComparison.OrdinalIgnoreCase) ? "series" : "movie";

        var baseQuery = _db.Titles
            .AsNoTracking()
            .Where(t => t.IsActive && t.Type == type);

        // Filter by Genre
        if (query.GenreId.HasValue && query.GenreId.Value > 0)
        {
            baseQuery = baseQuery.Where(t => t.TitleGenres.Any(tg => tg.GenreId == query.GenreId.Value));
        }

        // Filter by Year
        if (query.Year.HasValue && query.Year.Value > 1900)
        {
            if (type == "movie")
            {
                baseQuery = baseQuery.Where(t => t.ReleaseDate != null && t.ReleaseDate.Value.Year == query.Year.Value);
            }
            else
            {
                baseQuery = baseQuery.Where(t => t.FirstAirDate != null && t.FirstAirDate.Value.Year == query.Year.Value);
            }
        }

        // Filter by Language
        if (!string.IsNullOrWhiteSpace(query.Language))
        {
            baseQuery = baseQuery.Where(t => t.OriginalLanguage == query.Language.Trim().ToLowerInvariant());
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        IQueryable<Title> queryable = baseQuery
            .Include(t => t.TitleGenres)
                .ThenInclude(tg => tg.Genre);

        // Sorting
        queryable = (query.Sort?.ToLowerInvariant()) switch
        {
            "rating.desc" or "rating" or "top_rated" => queryable.OrderByDescending(t => t.TmdbRating).ThenByDescending(t => t.Popularity),
            "release_date.desc" or "newest" or "upcoming" => type == "series" 
                ? queryable.OrderByDescending(t => t.FirstAirDate).ThenByDescending(t => t.Popularity)
                : queryable.OrderByDescending(t => t.ReleaseDate).ThenByDescending(t => t.Popularity),
            "title.asc" or "az" => queryable.OrderBy(t => t.TitleText),
            _ => queryable.OrderByDescending(t => t.Popularity) // default popularity.desc
        };

        var titles = await queryable
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = titles.Select(t => new MediaCardDto(
            TmdbId: t.TmdbId,
            Type: t.Type,
            Title: t.TitleText,
            PosterPath: t.PosterPath,
            BackdropPath: t.BackdropPath,
            Rating: (double)(t.TmdbRating ?? 0m),
            Year: t.ReleaseDate?.Year.ToString() ?? t.FirstAirDate?.Year.ToString() ?? string.Empty,
            Genres: t.TitleGenres.Select(tg => tg.Genre.Name).Take(3).ToList()
        )).ToList();

        return new PagedResult<MediaCardDto>(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<List<GenreDto>> GetGenresAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cacheService.GetAsync<List<GenreDto>>(GenresCacheKey, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        var genres = await _db.Genres
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .Select(g => new GenreDto(g.Id, g.Name, g.Slug))
            .ToListAsync(cancellationToken);

        if (genres.Count > 0)
        {
            await _cacheService.SetAsync(GenresCacheKey, genres, GenresCacheDuration, cancellationToken);
        }

        return genres;
    }

    public async Task<PagedResult<MediaCardDto>> SearchTitlesAsync(SearchTitlesQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 24 : query.PageSize;

        var baseQuery = _db.Titles
            .AsNoTracking()
            .Where(t => t.IsActive);

        var normalizedType = string.IsNullOrWhiteSpace(query.Type) || string.Equals(query.Type, "all", StringComparison.OrdinalIgnoreCase)
            ? "all"
            : (string.Equals(query.Type, "series", StringComparison.OrdinalIgnoreCase) || string.Equals(query.Type, "tv", StringComparison.OrdinalIgnoreCase) ? "series" : "movie");

        if (normalizedType != "all")
        {
            baseQuery = baseQuery.Where(t => t.Type == normalizedType);
        }

        var searchByNorm = string.IsNullOrWhiteSpace(query.SearchBy)
            ? "all"
            : query.SearchBy.Trim().ToLowerInvariant();

        var searchTitle = searchByNorm == "all" || searchByNorm == "title";
        var searchCast = searchByNorm == "all" || searchByNorm == "cast" || searchByNorm == "actor";

        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            var searchTerm = query.Query.Trim();
            var searchPattern = $"%{searchTerm}%";
            string? dottedPattern = null;
            string? spacedDottedPattern = null;
            if (!searchTerm.Contains('.'))
            {
                var words = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var isAcronymCandidate = words.Any(w => w.Length is >= 2 and <= 5 && w.All(c => char.IsUpper(c) || char.IsDigit(c)));
                if (isAcronymCandidate)
                {
                    var transformed = words.Select(w => w.Length is >= 2 and <= 5 && w.All(c => char.IsUpper(c) || char.IsDigit(c)) ? string.Join(".", w.ToCharArray()) : w);
                    dottedPattern = $"%{string.Join("%", transformed)}%";
                    if (words.Length == 1 && words[0].Length is >= 2 and <= 5)
                    {
                        spacedDottedPattern = $"%{string.Join(". ", words[0].ToCharArray())}%";
                    }
                }
            }

            if (searchTitle && searchCast)
            {
                baseQuery = baseQuery.Where(t => 
                    EF.Functions.Like(t.TitleText, searchPattern) ||
                    (t.OriginalTitle != null && EF.Functions.Like(t.OriginalTitle, searchPattern)) ||
                    (dottedPattern != null && (
                        EF.Functions.Like(t.TitleText, dottedPattern) ||
                        (t.OriginalTitle != null && EF.Functions.Like(t.OriginalTitle, dottedPattern))
                    )) ||
                    (spacedDottedPattern != null && (
                        EF.Functions.Like(t.TitleText, spacedDottedPattern) ||
                        (t.OriginalTitle != null && EF.Functions.Like(t.OriginalTitle, spacedDottedPattern))
                    )) ||
                    t.TitleCasts.Any(tc => 
                        EF.Functions.Like(tc.Person.Name, searchPattern) ||
                        (tc.Character != null && EF.Functions.Like(tc.Character, searchPattern))
                    ));
            }
            else if (searchCast)
            {
                baseQuery = baseQuery.Where(t => 
                    t.TitleCasts.Any(tc => 
                        EF.Functions.Like(tc.Person.Name, searchPattern) ||
                        (tc.Character != null && EF.Functions.Like(tc.Character, searchPattern))
                    ));
            }
            else
            {
                baseQuery = baseQuery.Where(t => 
                    EF.Functions.Like(t.TitleText, searchPattern) ||
                    (t.OriginalTitle != null && EF.Functions.Like(t.OriginalTitle, searchPattern)) ||
                    (dottedPattern != null && (
                        EF.Functions.Like(t.TitleText, dottedPattern) ||
                        (t.OriginalTitle != null && EF.Functions.Like(t.OriginalTitle, dottedPattern))
                    )) ||
                    (spacedDottedPattern != null && (
                        EF.Functions.Like(t.TitleText, spacedDottedPattern) ||
                        (t.OriginalTitle != null && EF.Functions.Like(t.OriginalTitle, spacedDottedPattern))
                    )));
            }
        }

        var localTotalCount = await baseQuery.CountAsync(cancellationToken);

        // Fetch external TMDB titles if query term is provided
        List<MediaCardDto> externalTmdbItems = [];
        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            try
            {
                var searchTerm = query.Query.Trim();
                var tmdbCacheKey = $"tmdb_search_{normalizedType}_{searchByNorm}_{searchTerm.ToLowerInvariant()}";
                var cached = await _cacheService.GetAsync<List<MediaCardDto>>(tmdbCacheKey, cancellationToken);
                if (cached != null)
                {
                    externalTmdbItems = cached;
                }
                else
                {
                    var tmdbResults = await _tmdbService.SearchTmdbAsync(searchTerm, normalizedType, searchByNorm, cancellationToken);
                    if (tmdbResults.Count > 0)
                    {
                        externalTmdbItems = tmdbResults
                            .DistinctBy(r => r.TmdbId)
                            .Select(r => new MediaCardDto(
                                TmdbId: r.TmdbId,
                                Type: r.Type,
                                Title: r.Title,
                                PosterPath: r.PosterPath,
                                BackdropPath: r.BackdropPath,
                                Rating: r.Rating,
                                Year: r.Year ?? string.Empty,
                                Genres: r.Genres ?? [],
                                IsExternal: true
                            )).ToList();

                        await _cacheService.SetAsync(tmdbCacheKey, externalTmdbItems, TimeSpan.FromMinutes(30), cancellationToken);
                    }
                }

                // Deduplicate: exclude external TMDB items that already exist in our local database
                if (externalTmdbItems.Count > 0)
                {
                    var tmdbIds = externalTmdbItems.Select(e => e.TmdbId).Distinct().ToList();
                    var existingIds = await _db.Titles
                        .AsNoTracking()
                        .Where(t => t.IsActive && tmdbIds.Contains(t.TmdbId))
                        .Select(t => t.TmdbId)
                        .ToListAsync(cancellationToken);

                    if (existingIds.Count > 0)
                    {
                        var existingSet = existingIds.ToHashSet();
                        externalTmdbItems = externalTmdbItems.Where(e => !existingSet.Contains(e.TmdbId)).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query TMDB for search query {Query}", query.Query);
            }
        }

        var totalCount = localTotalCount + externalTmdbItems.Count;
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var startIndex = (page - 1) * pageSize;
        var items = new List<MediaCardDto>();

        if (startIndex < localTotalCount)
        {
            var localSkip = startIndex;
            var localTake = Math.Min(pageSize, localTotalCount - localSkip);

            var queryable = baseQuery
                .AsSplitQuery()
                .Include(t => t.TitleGenres)
                    .ThenInclude(tg => tg.Genre)
                .OrderByDescending(t => t.Popularity)
                .ThenByDescending(t => t.TmdbRating);

            var titles = await queryable
                .Skip(localSkip)
                .Take(localTake)
                .ToListAsync(cancellationToken);

            items.AddRange(titles.Select(t => new MediaCardDto(
                TmdbId: t.TmdbId,
                Type: t.Type,
                Title: t.TitleText,
                PosterPath: t.PosterPath,
                BackdropPath: t.BackdropPath,
                Rating: (double)(t.TmdbRating ?? 0m),
                Year: t.ReleaseDate?.Year.ToString() ?? t.FirstAirDate?.Year.ToString() ?? string.Empty,
                Genres: t.TitleGenres.Select(tg => tg.Genre.Name).Take(3).ToList(),
                IsExternal: false
            )));

            var neededFromExternal = pageSize - items.Count;
            if (neededFromExternal > 0 && externalTmdbItems.Count > 0)
            {
                items.AddRange(externalTmdbItems.Take(neededFromExternal));
            }
        }
        else
        {
            var externalSkip = startIndex - localTotalCount;
            if (externalSkip < externalTmdbItems.Count)
            {
                var externalTake = Math.Min(pageSize, externalTmdbItems.Count - externalSkip);
                items.AddRange(externalTmdbItems.Skip(externalSkip).Take(externalTake));
            }
        }

        return new PagedResult<MediaCardDto>(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<List<SearchSuggestionDto>> GetSearchSuggestionsAsync(string query, int limit = 6, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return [];
        }

        var searchTerm = query.Trim();
        var safeLimit = limit is < 1 or > 20 ? 6 : limit;
        var cacheKey = $"suggest_{safeLimit}_{searchTerm.ToLowerInvariant()}";

        var cached = await _cacheService.GetAsync<List<SearchSuggestionDto>>(cacheKey, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        var searchPattern = $"%{searchTerm}%";
        string? dottedPattern = null;
        string? spacedDottedPattern = null;
        if (!searchTerm.Contains('.'))
        {
            var words = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var isAcronymCandidate = words.Any(w => w.Length is >= 2 and <= 5 && w.All(c => char.IsUpper(c) || char.IsDigit(c)));
            if (isAcronymCandidate)
            {
                var transformed = words.Select(w => w.Length is >= 2 and <= 5 && w.All(c => char.IsUpper(c) || char.IsDigit(c)) ? string.Join(".", w.ToCharArray()) : w);
                dottedPattern = $"%{string.Join("%", transformed)}%";
                if (words.Length == 1 && words[0].Length is >= 2 and <= 5)
                {
                    spacedDottedPattern = $"%{string.Join(". ", words[0].ToCharArray())}%";
                }
            }
        }

        // Fetch up to 25 local candidates for relevance scoring in memory
        var candidates = await _db.Titles
            .AsNoTracking()
            .Include(t => t.TitleGenres)
                .ThenInclude(tg => tg.Genre)
            .Where(t => t.IsActive && (
                EF.Functions.Like(t.TitleText, searchPattern) ||
                (t.OriginalTitle != null && EF.Functions.Like(t.OriginalTitle, searchPattern)) ||
                (dottedPattern != null && (
                    EF.Functions.Like(t.TitleText, dottedPattern) ||
                    (t.OriginalTitle != null && EF.Functions.Like(t.OriginalTitle, dottedPattern))
                )) ||
                (spacedDottedPattern != null && (
                    EF.Functions.Like(t.TitleText, spacedDottedPattern) ||
                    (t.OriginalTitle != null && EF.Functions.Like(t.OriginalTitle, spacedDottedPattern))
                )) ||
                t.TitleCasts.Any(tc => 
                    EF.Functions.Like(tc.Person.Name, searchPattern) ||
                    (tc.Character != null && EF.Functions.Like(tc.Character, searchPattern))
                )
            ))
            .OrderByDescending(t => t.Popularity)
            .Take(25)
            .ToListAsync(cancellationToken);

        int ScoreTitleSuggestion(Title t, string term)
        {
            var title = t.TitleText ?? string.Empty;
            if (string.Equals(title, term, StringComparison.OrdinalIgnoreCase)) return 1000;
            if (title.StartsWith(term, StringComparison.OrdinalIgnoreCase)) return 800;
            if (title.Contains(" " + term, StringComparison.OrdinalIgnoreCase)) return 600;
            if (title.Contains(term, StringComparison.OrdinalIgnoreCase)) return 400;
            if (t.OriginalTitle != null && string.Equals(t.OriginalTitle, term, StringComparison.OrdinalIgnoreCase)) return 350;
            if (t.OriginalTitle != null && t.OriginalTitle.StartsWith(term, StringComparison.OrdinalIgnoreCase)) return 300;
            if (t.OriginalTitle != null && t.OriginalTitle.Contains(term, StringComparison.OrdinalIgnoreCase)) return 200;
            return 100;
        }

        var results = candidates
            .Select(t => (Title: t, Score: ScoreTitleSuggestion(t, searchTerm)))
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Title.Popularity)
            .ThenByDescending(x => x.Title.TmdbRating)
            .Take(safeLimit)
            .Select(x => new SearchSuggestionDto(
                TmdbId: x.Title.TmdbId,
                Type: x.Title.Type,
                Title: x.Title.TitleText,
                PosterPath: x.Title.PosterPath,
                Rating: (double)(x.Title.TmdbRating ?? 0m),
                Year: x.Title.ReleaseDate?.Year.ToString() ?? x.Title.FirstAirDate?.Year.ToString() ?? string.Empty,
                Genres: x.Title.TitleGenres.Select(tg => tg.Genre.Name).Take(2).ToList(),
                IsExternal: false
            ))
            .ToList();

        // Fill remaining suggestion slots from TMDB if local suggestions are fewer than requested limit
        if (results.Count < safeLimit)
        {
            try
            {
                var existingTmdbIds = results.Select(r => r.TmdbId).ToHashSet();
                var tmdbResults = await _tmdbService.SearchTmdbAsync(searchTerm, null, "all", cancellationToken);
                if (tmdbResults.Count > 0)
                {
                    int ScoreTmdbSuggestion(TmdbSearchResultItemDto item, string term)
                    {
                        var title = item.Title ?? string.Empty;
                        if (string.Equals(title, term, StringComparison.OrdinalIgnoreCase)) return 1000;
                        if (title.StartsWith(term, StringComparison.OrdinalIgnoreCase)) return 800;
                        if (title.Contains(" " + term, StringComparison.OrdinalIgnoreCase)) return 600;
                        if (title.Contains(term, StringComparison.OrdinalIgnoreCase)) return 400;
                        return 200;
                    }

                    var additional = tmdbResults
                        .Where(r => !existingTmdbIds.Contains(r.TmdbId))
                        .DistinctBy(r => r.TmdbId)
                        .Select(r => (Item: r, Score: ScoreTmdbSuggestion(r, searchTerm)))
                        .OrderByDescending(x => x.Score)
                        .ThenByDescending(x => x.Item.Popularity)
                        .Take(safeLimit - results.Count)
                        .Select(x => new SearchSuggestionDto(
                            TmdbId: x.Item.TmdbId,
                            Type: x.Item.Type,
                            Title: x.Item.Title,
                            PosterPath: x.Item.PosterPath,
                            Rating: x.Item.Rating,
                            Year: x.Item.Year ?? string.Empty,
                            Genres: x.Item.Genres?.Take(2).ToList() ?? [],
                            IsExternal: true
                        ));
                    results.AddRange(additional);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query TMDB suggestions for {Query}", searchTerm);
            }
        }

        if (results.Count > 0)
        {
            await _cacheService.SetAsync(cacheKey, results, TimeSpan.FromMinutes(15), cancellationToken);
        }

        return results;
    }

    private async Task<List<MediaCardDto>> GetTitlesInternalAsync(string category, string type, int limit, CancellationToken cancellationToken)
    {
        var query = _db.Titles
            .AsNoTracking()
            .Include(t => t.TitleGenres)
                .ThenInclude(tg => tg.Genre)
            .Where(t => t.IsActive && t.Type == type);

        if (category == "top_rated")
        {
            query = query
                .Where(t => (t.TmdbListCategory != null && t.TmdbListCategory.Contains("top_rated")) || (t.TmdbRating != null && t.TmdbRating >= 7.0m))
                .OrderByDescending(t => t.TmdbRating)
                .ThenByDescending(t => t.Popularity);
        }
        else if (category == "upcoming")
        {
            query = query
                .Where(t => (t.TmdbListCategory != null && t.TmdbListCategory.Contains("upcoming")) || (t.ReleaseDate != null && t.ReleaseDate >= DateTime.UtcNow.AddMonths(-3)))
                .OrderByDescending(t => t.ReleaseDate)
                .ThenByDescending(t => t.Popularity);
        }
        else
        {
            // trending or popular
            query = query
                .Where(t => t.TmdbListCategory != null && t.TmdbListCategory.Contains(category))
                .OrderByDescending(t => t.Popularity);
        }

        var titles = await query.Take(limit).ToListAsync(cancellationToken);

        // Fallback: If no titles tagged with category yet, return any active titles of that type
        if (titles.Count == 0)
        {
            titles = await _db.Titles
                .AsNoTracking()
                .Include(t => t.TitleGenres)
                    .ThenInclude(tg => tg.Genre)
                .Where(t => t.IsActive && t.Type == type)
                .OrderByDescending(t => t.Popularity)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        return titles.Select(t => new MediaCardDto(
            TmdbId: t.TmdbId,
            Type: t.Type,
            Title: t.TitleText,
            PosterPath: t.PosterPath,
            BackdropPath: t.BackdropPath,
            Rating: (double)(t.TmdbRating ?? 0m),
            Year: t.ReleaseDate?.Year.ToString() ?? t.FirstAirDate?.Year.ToString() ?? string.Empty,
            Genres: t.TitleGenres.Select(tg => tg.Genre.Name).Take(3).ToList()
        )).ToList();
    }

    public async Task<TitleDetailDto?> GetTitleDetailAsync(int id, string? type = null, CancellationToken cancellationToken = default)
    {
        var title = await _db.Titles
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.TitleGenres)
                .ThenInclude(tg => tg.Genre)
            .Include(t => t.TitleCasts)
                .ThenInclude(tc => tc.Person)
            .Include(t => t.Videos)
            .Include(t => t.TitleProviders)
                .ThenInclude(tp => tp.Provider)
            .Include(t => t.Seasons)
                .ThenInclude(s => s.Episodes)
            .FirstOrDefaultAsync(t => t.IsActive && (t.Id == id || t.TmdbId == id), cancellationToken);

        if (title == null)
        {
            // Title not in database! Import on demand from TMDB
            _logger.LogInformation("Title {Id} not found in database. Attempting on-demand import from TMDB...", id);

            var resolvedType = type;
            if (string.IsNullOrWhiteSpace(resolvedType) || string.Equals(resolvedType, "title", StringComparison.OrdinalIgnoreCase))
            {
                var movieCheck = await _tmdbService.GetFullDetailsAsync(id, "movie", cancellationToken);
                if (movieCheck != null)
                {
                    resolvedType = "movie";
                }
                else
                {
                    var tvCheck = await _tmdbService.GetFullDetailsAsync(id, "series", cancellationToken);
                    if (tvCheck != null)
                    {
                        resolvedType = "series";
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(resolvedType))
            {
                var imported = await _tmdbSyncService.SyncSingleTitleAsync(id, resolvedType, cancellationToken);
                if (imported)
                {
                    _logger.LogInformation("Successfully imported TMDB {Type} {Id} into database.", resolvedType, id);
                    title = await _db.Titles
                        .AsNoTracking()
                        .AsSplitQuery()
                        .Include(t => t.TitleGenres)
                            .ThenInclude(tg => tg.Genre)
                        .Include(t => t.TitleCasts)
                            .ThenInclude(tc => tc.Person)
                        .Include(t => t.Videos)
                        .Include(t => t.TitleProviders)
                            .ThenInclude(tp => tp.Provider)
                        .Include(t => t.Seasons)
                            .ThenInclude(s => s.Episodes)
                        .FirstOrDefaultAsync(t => t.IsActive && (t.Id == id || t.TmdbId == id), cancellationToken);
                }
            }
        }

        if (title == null)
        {
            return null;
        }

        var cast = title.TitleCasts
            .OrderBy(tc => tc.DisplayOrder)
            .Take(15)
            .Select(tc => new CastMemberDto(
                PersonId: tc.PersonId,
                TmdbId: tc.Person.TmdbId,
                Name: tc.Person.Name,
                Character: tc.Character,
                ProfilePath: tc.Person.ProfilePath,
                DisplayOrder: tc.DisplayOrder
            ))
            .ToList();

        var videos = title.Videos
            .Select(v => new VideoDto(
                Id: v.Id,
                Key: v.TmdbKey,
                Name: v.Name,
                Type: v.Type,
                Site: v.Site
            ))
            .ToList();

        var providers = title.TitleProviders
            .OrderBy(tp => tp.Provider.Name)
            .Select(tp => new WatchProviderDto(
                ProviderId: tp.ProviderId,
                TmdbId: tp.Provider.TmdbId,
                Name: tp.Provider.Name,
                LogoPath: tp.Provider.LogoPath,
                Type: tp.Type,
                Country: tp.Country
            ))
            .ToList();

        var seasons = title.Seasons
            .OrderBy(s => s.SeasonNumber)
            .Select(s => new SeasonDto(
                Id: s.Id,
                TmdbId: s.TmdbId,
                SeasonNumber: s.SeasonNumber,
                Name: s.Name,
                Overview: s.Overview,
                PosterPath: s.PosterPath,
                AirDate: s.AirDate?.ToString("yyyy-MM-dd"),
                EpisodeCount: s.EpisodeCount,
                Episodes: MapEpisodes(s.Episodes, s)
            ))
            .ToList();

        return new TitleDetailDto(
            Id: title.Id,
            TmdbId: title.TmdbId,
            Type: title.Type,
            Title: title.TitleText,
            OriginalTitle: title.OriginalTitle,
            Tagline: title.Tagline,
            Overview: title.Overview,
            PosterPath: title.PosterPath,
            BackdropPath: title.BackdropPath,
            Rating: (double)(title.TmdbRating ?? 0m),
            VoteCount: title.VoteCount,
            ReleaseDate: title.ReleaseDate?.ToString("yyyy-MM-dd") ?? title.FirstAirDate?.ToString("yyyy-MM-dd"),
            Year: title.ReleaseDate?.Year.ToString() ?? title.FirstAirDate?.Year.ToString() ?? string.Empty,
            RuntimeMinutes: title.RuntimeMinutes,
            Status: title.Status,
            ContentRating: title.ContentRating,
            OriginalLanguage: title.OriginalLanguage,
            TotalSeasons: title.TotalSeasons,
            TotalEpisodes: title.TotalEpisodes,
            Genres: title.TitleGenres.Select(tg => tg.Genre.Name).ToList(),
            Cast: cast,
            Videos: videos,
            Providers: providers,
            Seasons: seasons
        );
    }

    public async Task<List<SeasonDto>> GetTitleSeasonsAsync(int id, CancellationToken cancellationToken = default)
    {
        var title = await _db.Titles
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IsActive && (t.Id == id || t.TmdbId == id), cancellationToken);

        if (title == null)
        {
            return [];
        }

        var seasons = await _db.Seasons
            .AsNoTracking()
            .Include(s => s.Episodes)
            .Where(s => s.TitleId == title.Id)
            .OrderBy(s => s.SeasonNumber)
            .ToListAsync(cancellationToken);

        return seasons.Select(s => new SeasonDto(
            Id: s.Id,
            TmdbId: s.TmdbId,
            SeasonNumber: s.SeasonNumber,
            Name: s.Name,
            Overview: s.Overview,
            PosterPath: s.PosterPath,
            AirDate: s.AirDate?.ToString("yyyy-MM-dd"),
            EpisodeCount: s.EpisodeCount,
            Episodes: MapEpisodes(s.Episodes, s)
        )).ToList();
    }

    private static List<EpisodeDto> MapEpisodes(ICollection<Episode> episodes, Season season)
    {
        if (episodes != null && episodes.Count > 0)
        {
            return episodes
                .OrderBy(e => e.EpisodeNumber)
                .Select(e => new EpisodeDto(
                    Id: e.Id,
                    TmdbId: e.TmdbId,
                    EpisodeNumber: e.EpisodeNumber,
                    Name: string.IsNullOrWhiteSpace(e.Name) ? $"Episode {e.EpisodeNumber}" : e.Name,
                    Overview: e.Overview,
                    StillPath: e.StillPath ?? season.PosterPath,
                    AirDate: e.AirDate?.ToString("yyyy-MM-dd"),
                    RuntimeMinutes: e.RuntimeMinutes
                ))
                .ToList();
        }

        if (season.EpisodeCount > 0)
        {
            return Enumerable.Range(1, season.EpisodeCount)
                .Select(epNum => new EpisodeDto(
                    Id: epNum,
                    TmdbId: season.TmdbId * 100 + epNum,
                    EpisodeNumber: epNum,
                    Name: $"Episode {epNum}",
                    Overview: null,
                    StillPath: season.PosterPath,
                    AirDate: season.AirDate?.ToString("yyyy-MM-dd"),
                    RuntimeMinutes: null
                ))
                .ToList();
        }

        return [];
    }

    public async Task<List<WatchProviderDto>> GetTitleWhereToWatchAsync(int id, CancellationToken cancellationToken = default)
    {
        var title = await _db.Titles
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IsActive && (t.Id == id || t.TmdbId == id), cancellationToken);

        if (title == null)
        {
            return [];
        }

        var providers = await _db.TitleProviders
            .AsNoTracking()
            .Include(tp => tp.Provider)
            .Where(tp => tp.TitleId == title.Id)
            .OrderBy(tp => tp.Provider.Name)
            .ToListAsync(cancellationToken);

        return providers.Select(tp => new WatchProviderDto(
            ProviderId: tp.ProviderId,
            TmdbId: tp.Provider.TmdbId,
            Name: tp.Provider.Name,
            LogoPath: tp.Provider.LogoPath,
            Type: tp.Type,
            Country: tp.Country
        )).ToList();
    }

    public async Task<List<TitleSourceDto>> GetTitleSourcesAsync(int titleId, int? season = null, int? episode = null, string? type = null, CancellationToken cancellationToken = default)
    {
        var title = await _db.Titles
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IsActive && (t.Id == titleId || t.TmdbId == titleId), cancellationToken);

        if (title == null)
        {
            var resolvedType = !string.IsNullOrWhiteSpace(type) ? type : (season.HasValue ? "series" : "movie");
            _logger.LogInformation("Title {TitleId} sources requested but title not in DB. Attempting import as {Type}...", titleId, resolvedType);
            await _tmdbSyncService.SyncSingleTitleAsync(titleId, resolvedType, cancellationToken);

            title = await _db.Titles
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IsActive && (t.Id == titleId || t.TmdbId == titleId), cancellationToken);

            if (title == null)
            {
                return [];
            }
        }

        var sourcesQuery = _db.TitleSources
            .AsNoTracking()
            .Include(ts => ts.SourceProvider)
            .Where(ts => ts.TitleId == title.Id && ts.IsActive);

        if (string.Equals(title.Type, "series", StringComparison.OrdinalIgnoreCase) && season.HasValue && episode.HasValue)
        {
            sourcesQuery = sourcesQuery.Where(ts => 
                (ts.SeasonNumber == season.Value && ts.EpisodeNumber == episode.Value) || 
                (ts.SeasonNumber == null && ts.EpisodeNumber == null));
        }

        var dbSources = await sourcesQuery
            .OrderBy(ts => ts.Priority)
            .ToListAsync(cancellationToken);

        var result = dbSources.Select(ts => new TitleSourceDto(
            Id: ts.Id,
            ProviderName: ts.SourceProvider.Name,
            SourceType: ts.SourceProvider.Type,
            Url: ts.EmbedUrl ?? ts.StreamUrl ?? string.Empty,
            Quality: ts.Quality,
            Label: string.IsNullOrWhiteSpace(ts.SourceProvider.Name) ? $"Server {ts.Priority}" : $"{ts.SourceProvider.Name} ({ts.Quality})",
            Priority: !string.IsNullOrEmpty(ts.StreamUrl) ? 0 : (ts.SourceProvider.Name.Equals("VidSrc", StringComparison.OrdinalIgnoreCase) ? 2 : ts.Priority),
            IsDirectStream: !string.IsNullOrEmpty(ts.StreamUrl)
        )).ToList();

        // Ensure multiple verified fallback servers are always available so users have options if one buffers
        var isSeries = string.Equals(title.Type, "series", StringComparison.OrdinalIgnoreCase);
        var s = season ?? 1;
        var e = episode ?? 1;

        var standardSources = isSeries
            ? new List<TitleSourceDto>
            {
                new(101, "VidLink", "ThirdPartyEmbed", $"https://vidlink.pro/tv/{title.TmdbId}/{s}/{e}", "1080p HD", "Server 1 (VidLink HD)", 1, false),
                new(102, "VidSrc", "ThirdPartyEmbed", $"https://vidsrc.to/embed/tv/{title.TmdbId}/{s}/{e}", "1080p HD", "Server 2 (VidSrc Pro)", 2, false),
                new(103, "2Embed", "ThirdPartyEmbed", $"https://www.2embed.cc/embedtv/{title.TmdbId}&s={s}&e={e}", "Auto", "Server 3 (2Embed)", 3, false),
                new(104, "VidSrc Mirror", "ThirdPartyEmbed", $"https://vidsrc.pm/embed/tv/{title.TmdbId}/{s}/{e}", "720p/1080p", "Server 4 (VidSrc Mirror)", 4, false),
                new(105, "AutoEmbed", "ThirdPartyEmbed", $"https://autoembed.co/tv/tmdb/{title.TmdbId}-{s}-{e}", "1080p", "Server 5 (AutoEmbed)", 5, false),
                new(106, "SmashyStream", "ThirdPartyEmbed", $"https://player.smashystream.com/playere.php?tmdb={title.TmdbId}&season={s}&episode={e}", "1080p/Multi", "Server 6 (SmashyStream)", 6, false)
            }
            : new List<TitleSourceDto>
            {
                new(101, "VidLink", "ThirdPartyEmbed", $"https://vidlink.pro/movie/{title.TmdbId}", "1080p HD", "Server 1 (VidLink HD)", 1, false),
                new(102, "VidSrc", "ThirdPartyEmbed", $"https://vidsrc.to/embed/movie/{title.TmdbId}", "1080p HD", "Server 2 (VidSrc Pro)", 2, false),
                new(103, "2Embed", "ThirdPartyEmbed", $"https://www.2embed.cc/embed/{title.TmdbId}", "Auto", "Server 3 (2Embed)", 3, false),
                new(104, "VidSrc Mirror", "ThirdPartyEmbed", $"https://vidsrc.pm/embed/movie/{title.TmdbId}", "720p/1080p", "Server 4 (VidSrc Mirror)", 4, false),
                new(105, "AutoEmbed", "ThirdPartyEmbed", $"https://autoembed.co/movie/tmdb/{title.TmdbId}", "1080p", "Server 5 (AutoEmbed)", 5, false),
                new(106, "SmashyStream", "ThirdPartyEmbed", $"https://player.smashystream.com/playere.php?tmdb={title.TmdbId}", "1080p/Multi", "Server 6 (SmashyStream)", 6, false)
            };

        foreach (var standard in standardSources)
        {
            if (!result.Any(r => r.ProviderName.Equals(standard.ProviderName, StringComparison.OrdinalIgnoreCase) || r.Url == standard.Url))
            {
                result.Add(standard);
            }
        }

        return result.OrderBy(r => r.Priority).ToList();
    }

    public async Task<SourceReportResponseDto> ReportSourceAsync(int sourceId, int titleId, string reason, string? details = null, int? season = null, int? episode = null, CancellationToken cancellationToken = default)
    {
        var title = await _db.Titles.FirstOrDefaultAsync(t => t.Id == titleId || t.TmdbId == titleId, cancellationToken);
        var resolvedTitleId = title?.Id ?? titleId;

        var source = await _db.TitleSources.FirstOrDefaultAsync(ts => ts.Id == sourceId, cancellationToken);
        int resolvedSourceId = sourceId;

        if (source != null)
        {
            source.LastVerifiedAt = DateTime.UtcNow;
            resolvedSourceId = source.Id;
        }
        else if (title != null)
        {
            var fallbackSource = await _db.TitleSources.FirstOrDefaultAsync(ts => ts.TitleId == title.Id, cancellationToken);
            if (fallbackSource != null)
            {
                resolvedSourceId = fallbackSource.Id;
            }
            else
            {
                var defaultProvider = await _db.SourceProviders.FirstOrDefaultAsync(cancellationToken);
                var newSource = new TitleSource
                {
                    TitleId = title.Id,
                    SourceProviderId = defaultProvider?.Id ?? 1,
                    Quality = "1080p",
                    Priority = 1,
                    EmbedUrl = $"https://vidsrc.to/embed/movie/{title.TmdbId}",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.TitleSources.Add(newSource);
                await _db.SaveChangesAsync(cancellationToken);
                resolvedSourceId = newSource.Id;
            }
        }

        var report = new SourceReport
        {
            SourceId = resolvedSourceId,
            TitleId = resolvedTitleId,
            Reason = reason,
            Details = details,
            Season = season,
            Episode = episode,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.SourceReports.Add(report);
        await _db.SaveChangesAsync(cancellationToken);

        return new SourceReportResponseDto(true, "Thank you! Your report has been submitted to our streaming team.");
    }

    public async Task<List<MediaCardDto>> GetTitleRecommendationsAsync(int id, int limit = 12, CancellationToken cancellationToken = default)
    {
        var safeLimit = limit is < 1 or > 30 ? 12 : limit;
        var cacheKey = $"title_recs_{id}_{safeLimit}";
        var cached = await _cacheService.GetAsync<List<MediaCardDto>>(cacheKey, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        // 1. Resolve target title
        var title = await _db.Titles
            .AsNoTracking()
            .Include(t => t.TitleGenres)
                .ThenInclude(tg => tg.Genre)
            .Include(t => t.TitleCasts)
            .FirstOrDefaultAsync(t => (t.Id == id || t.TmdbId == id) && t.IsActive, cancellationToken);

        var resultList = new List<MediaCardDto>();
        var seenTmdbIds = new HashSet<int>();
        var targetTmdbId = title?.TmdbId ?? id;
        seenTmdbIds.Add(targetTmdbId);

        // 2. Query TMDB Recommendations (collaborative filtering & deep film similarity)
        var targetType = title?.Type ?? "movie";
        List<MoviesCenter.Application.Features.Admin.Dtos.TmdbSearchResultItemDto> tmdbRecs = [];
        try
        {
            tmdbRecs = await _tmdbService.GetRecommendationsAsync(targetTmdbId, targetType, cancellationToken);
            if (tmdbRecs.Count == 0 && title == null)
            {
                // If title was not found locally and type is unknown, try series as fallback
                tmdbRecs = await _tmdbService.GetRecommendationsAsync(targetTmdbId, "series", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch TMDB recommendations for title ID {Id}", id);
        }

        if (tmdbRecs.Count > 0)
        {
            var recTmdbIds = tmdbRecs.Select(r => r.TmdbId).Distinct().ToList();
            var localMatches = await _db.Titles
                .AsNoTracking()
                .Include(t => t.TitleGenres)
                    .ThenInclude(tg => tg.Genre)
                .Where(t => t.IsActive && recTmdbIds.Contains(t.TmdbId))
                .ToDictionaryAsync(t => t.TmdbId, cancellationToken);

            foreach (var rec in tmdbRecs)
            {
                if (!seenTmdbIds.Add(rec.TmdbId)) continue;

                if (localMatches.TryGetValue(rec.TmdbId, out var local))
                {
                    resultList.Add(new MediaCardDto(
                        TmdbId: local.TmdbId,
                        Type: local.Type,
                        Title: local.TitleText,
                        PosterPath: local.PosterPath,
                        BackdropPath: local.BackdropPath,
                        Rating: (double)(local.TmdbRating ?? 0m),
                        Year: local.ReleaseDate?.Year.ToString() ?? local.FirstAirDate?.Year.ToString() ?? string.Empty,
                        Genres: local.TitleGenres.Select(tg => tg.Genre.Name).Take(3).ToList(),
                        IsExternal: false
                    ));
                }
                else
                {
                    resultList.Add(new MediaCardDto(
                        TmdbId: rec.TmdbId,
                        Type: rec.Type,
                        Title: rec.Title,
                        PosterPath: rec.PosterPath,
                        BackdropPath: rec.BackdropPath,
                        Rating: rec.Rating,
                        Year: rec.Year ?? string.Empty,
                        Genres: rec.Genres ?? [],
                        IsExternal: true
                    ));
                }

                if (resultList.Count >= safeLimit) break;
            }
        }

        // 3. Fallback / Supplement: High-accuracy local matching using shared actors, multiple genres & era proximity
        if (resultList.Count < safeLimit && title != null)
        {
            var targetGenreIds = title.TitleGenres.Select(tg => tg.GenreId).ToHashSet();
            var targetPersonIds = title.TitleCasts.Select(tc => tc.PersonId).ToHashSet();
            var targetYear = title.ReleaseDate?.Year ?? title.FirstAirDate?.Year;

            if (targetGenreIds.Count > 0 || targetPersonIds.Count > 0)
            {
                var candidates = await _db.Titles
                    .AsNoTracking()
                    .Include(t => t.TitleGenres)
                        .ThenInclude(tg => tg.Genre)
                    .Include(t => t.TitleCasts)
                    .Where(t => t.IsActive && t.Type == title.Type && t.Id != title.Id && !seenTmdbIds.Contains(t.TmdbId))
                    .Where(t => (targetGenreIds.Count > 0 && t.TitleGenres.Any(tg => targetGenreIds.Contains(tg.GenreId))) ||
                                (targetPersonIds.Count > 0 && t.TitleCasts.Any(tc => targetPersonIds.Contains(tc.PersonId))))
                    .Take(50)
                    .ToListAsync(cancellationToken);

                var scored = candidates
                    .Select(c => {
                        double score = 0;
                        var sharedCast = targetPersonIds.Count > 0 ? c.TitleCasts.Count(tc => targetPersonIds.Contains(tc.PersonId)) : 0;
                        score += sharedCast * 50.0;

                        var sharedGenres = targetGenreIds.Count > 0 ? c.TitleGenres.Count(tg => targetGenreIds.Contains(tg.GenreId)) : 0;
                        score += sharedGenres * 20.0;

                        var cYear = c.ReleaseDate?.Year ?? c.FirstAirDate?.Year;
                        if (targetYear.HasValue && cYear.HasValue)
                        {
                            var diff = Math.Abs(targetYear.Value - cYear.Value);
                            if (diff <= 3) score += 15.0;
                            else if (diff <= 7) score += 10.0;
                            else if (diff <= 12) score += 5.0;
                        }

                        var total = score + (double)(c.TmdbRating ?? 0m) + Math.Min(c.Popularity / 20.0, 10.0);
                        return (Item: c, TotalScore: total);
                    })
                    .OrderByDescending(x => x.TotalScore)
                    .Select(x => x.Item)
                    .Take(safeLimit - resultList.Count);

                foreach (var c in scored)
                {
                    if (seenTmdbIds.Add(c.TmdbId))
                    {
                        resultList.Add(new MediaCardDto(
                            TmdbId: c.TmdbId,
                            Type: c.Type,
                            Title: c.TitleText,
                            PosterPath: c.PosterPath,
                            BackdropPath: c.BackdropPath,
                            Rating: (double)(c.TmdbRating ?? 0m),
                            Year: c.ReleaseDate?.Year.ToString() ?? c.FirstAirDate?.Year.ToString() ?? string.Empty,
                            Genres: c.TitleGenres.Select(tg => tg.Genre.Name).Take(3).ToList(),
                            IsExternal: false
                        ));
                    }
                }
            }
        }

        // 4. Backfill with popular titles of same type only if still fewer than limit
        if (resultList.Count < safeLimit && title != null)
        {
            var remaining = safeLimit - resultList.Count;
            var backfill = await _db.Titles
                .AsNoTracking()
                .Include(t => t.TitleGenres)
                    .ThenInclude(tg => tg.Genre)
                .Where(t => t.IsActive && t.Type == title.Type && !seenTmdbIds.Contains(t.TmdbId))
                .OrderByDescending(t => t.Popularity)
                .ThenByDescending(t => t.TmdbRating)
                .Take(remaining)
                .ToListAsync(cancellationToken);

            resultList.AddRange(backfill.Select(t => new MediaCardDto(
                TmdbId: t.TmdbId,
                Type: t.Type,
                Title: t.TitleText,
                PosterPath: t.PosterPath,
                BackdropPath: t.BackdropPath,
                Rating: (double)(t.TmdbRating ?? 0m),
                Year: t.ReleaseDate?.Year.ToString() ?? t.FirstAirDate?.Year.ToString() ?? string.Empty,
                Genres: t.TitleGenres.Select(tg => tg.Genre.Name).Take(3).ToList(),
                IsExternal: false
            )));
        }

        if (resultList.Count > 0)
        {
            await _cacheService.SetAsync(cacheKey, resultList, TimeSpan.FromHours(6), cancellationToken);
        }

        return resultList;
    }

    public async Task<List<MediaCardDto>> GetTop10Async(string type = "all", CancellationToken cancellationToken = default)
    {
        var normalizedType = (type?.Trim().ToLowerInvariant()) switch
        {
            "movie" or "movies" => "movie",
            "series" or "tv" => "series",
            _ => "all"
        };

        var cacheKey = $"top10_{normalizedType}";
        var cached = await _cacheService.GetAsync<List<MediaCardDto>>(cacheKey, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        var query = _db.Titles
            .AsNoTracking()
            .Include(t => t.TitleGenres)
                .ThenInclude(tg => tg.Genre)
            .Where(t => t.IsActive);

        if (normalizedType == "movie" || normalizedType == "series")
        {
            query = query.Where(t => t.Type == normalizedType);
        }

        var titles = await query
            .OrderByDescending(t => t.Popularity)
            .ThenByDescending(t => t.ViewCount)
            .ThenByDescending(t => t.TmdbRating)
            .Take(10)
            .ToListAsync(cancellationToken);

        var result = titles.Select(t => new MediaCardDto(
            TmdbId: t.TmdbId,
            Type: t.Type,
            Title: t.TitleText,
            PosterPath: t.PosterPath,
            BackdropPath: t.BackdropPath,
            Rating: (double)(t.TmdbRating ?? 0m),
            Year: t.ReleaseDate?.Year.ToString() ?? t.FirstAirDate?.Year.ToString() ?? string.Empty,
            Genres: t.TitleGenres.Select(tg => tg.Genre.Name).Take(3).ToList()
        )).ToList();

        // 1-hour cache
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(1), cancellationToken);

        return result;
    }
}
