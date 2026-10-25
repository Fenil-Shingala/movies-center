using Microsoft.EntityFrameworkCore;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.Catalog.Dtos;
using MoviesCenter.Application.Features.Catalog.Queries;
using MoviesCenter.Application.Features.Home.Dtos;
using MoviesCenter.Application.Features.Search.Dtos;
using MoviesCenter.Application.Features.Search.Queries;
using MoviesCenter.Domain.Entities;
using MoviesCenter.Infrastructure.Data;

namespace MoviesCenter.Infrastructure.Services;

public class CatalogQueryService : ICatalogQueryService
{
    private readonly ApplicationDbContext _db;
    private readonly ICacheService _cacheService;
    private const string HomeFeedCacheKey = "home_feed_v1";
    private static readonly TimeSpan HomeFeedCacheDuration = TimeSpan.FromHours(1);
    private const string GenresCacheKey = "genres_all_v1";
    private static readonly TimeSpan GenresCacheDuration = TimeSpan.FromDays(1);

    public CatalogQueryService(ApplicationDbContext db, ICacheService cacheService)
    {
        _db = db;
        _cacheService = cacheService;
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
            new("trending-movies", "Trending Movies", "movie", "/movies?sort=trending", trendingMovies),
            new("trending-series", "Trending Series", "series", "/series?sort=trending", trendingSeries),
            new("popular-movies", "Popular Movies", "movie", "/movies?sort=popular", popularMovies),
            new("top-rated-movies", "Top Rated Movies", "movie", "/movies?sort=top_rated", topRatedMovies),
            new("popular-series", "Popular Series", "series", "/series?sort=popular", popularSeries),
            new("upcoming-movies", "Upcoming Movies", "movie", "/movies?sort=upcoming", upcomingMovies)
        };

        var feed = new HomeFeedDto(hero, rows);

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

        var queryable = _db.Titles
            .AsNoTracking()
            .Include(t => t.TitleGenres)
                .ThenInclude(tg => tg.Genre)
            .Where(t => t.IsActive && t.Type == type);

        // Filter by Genre
        if (query.GenreId.HasValue && query.GenreId.Value > 0)
        {
            queryable = queryable.Where(t => t.TitleGenres.Any(tg => tg.GenreId == query.GenreId.Value));
        }

        // Filter by Year
        if (query.Year.HasValue && query.Year.Value > 1900)
        {
            if (type == "movie")
            {
                queryable = queryable.Where(t => t.ReleaseDate != null && t.ReleaseDate.Value.Year == query.Year.Value);
            }
            else
            {
                queryable = queryable.Where(t => t.FirstAirDate != null && t.FirstAirDate.Value.Year == query.Year.Value);
            }
        }

        // Filter by Language
        if (!string.IsNullOrWhiteSpace(query.Language))
        {
            queryable = queryable.Where(t => t.OriginalLanguage == query.Language.Trim().ToLowerInvariant());
        }

        // Sorting
        queryable = (query.Sort?.ToLowerInvariant()) switch
        {
            "rating.desc" or "rating" => queryable.OrderByDescending(t => t.TmdbRating).ThenByDescending(t => t.Popularity),
            "release_date.desc" or "newest" => type == "series" 
                ? queryable.OrderByDescending(t => t.FirstAirDate).ThenByDescending(t => t.Popularity)
                : queryable.OrderByDescending(t => t.ReleaseDate).ThenByDescending(t => t.Popularity),
            "title.asc" or "az" => queryable.OrderBy(t => t.TitleText),
            _ => queryable.OrderByDescending(t => t.Popularity) // default popularity.desc
        };

        var totalCount = await queryable.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

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

        var queryable = _db.Titles
            .AsNoTracking()
            .Include(t => t.TitleGenres)
                .ThenInclude(tg => tg.Genre)
            .Where(t => t.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Type) && !string.Equals(query.Type, "all", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedType = string.Equals(query.Type, "series", StringComparison.OrdinalIgnoreCase) ? "series" : "movie";
            queryable = queryable.Where(t => t.Type == normalizedType);
        }

        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            var searchTerm = query.Query.Trim();
            queryable = queryable.Where(t => 
                EF.Functions.Like(t.TitleText, $"%{searchTerm}%") ||
                (t.OriginalTitle != null && EF.Functions.Like(t.OriginalTitle, $"%{searchTerm}%")));
        }

        queryable = queryable.OrderByDescending(t => t.Popularity).ThenByDescending(t => t.TmdbRating);

        var totalCount = await queryable.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

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

    public async Task<List<SearchSuggestionDto>> GetSearchSuggestionsAsync(string query, int limit = 6, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return [];
        }

        var searchTerm = query.Trim();
        var safeLimit = limit is < 1 or > 20 ? 6 : limit;

        var suggestions = await _db.Titles
            .AsNoTracking()
            .Include(t => t.TitleGenres)
                .ThenInclude(tg => tg.Genre)
            .Where(t => t.IsActive && (
                EF.Functions.Like(t.TitleText, $"%{searchTerm}%") ||
                (t.OriginalTitle != null && EF.Functions.Like(t.OriginalTitle, $"%{searchTerm}%"))
            ))
            .OrderByDescending(t => t.Popularity)
            .Take(safeLimit)
            .ToListAsync(cancellationToken);

        return suggestions.Select(t => new SearchSuggestionDto(
            TmdbId: t.TmdbId,
            Type: t.Type,
            Title: t.TitleText,
            PosterPath: t.PosterPath,
            Rating: (double)(t.TmdbRating ?? 0m),
            Year: t.ReleaseDate?.Year.ToString() ?? t.FirstAirDate?.Year.ToString() ?? string.Empty,
            Genres: t.TitleGenres.Select(tg => tg.Genre.Name).Take(2).ToList()
        )).ToList();
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
}
