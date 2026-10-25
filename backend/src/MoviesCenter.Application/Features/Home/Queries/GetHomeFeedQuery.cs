using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Home.Dtos;

namespace MoviesCenter.Application.Features.Home.Queries;

public record GetHomeFeedQuery : IRequest<HomeFeedDto>;

public class GetHomeFeedQueryHandler : IRequestHandler<GetHomeFeedQuery, HomeFeedDto>
{
    private readonly ITmdbService _tmdbService;
    private readonly ICacheService _cacheService;
    private const string CacheKey = "home_feed_v1";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public GetHomeFeedQueryHandler(ITmdbService tmdbService, ICacheService cacheService)
    {
        _tmdbService = tmdbService;
        _cacheService = cacheService;
    }

    public async Task<HomeFeedDto> Handle(GetHomeFeedQuery request, CancellationToken cancellationToken)
    {
        var cached = await _cacheService.GetAsync<HomeFeedDto>(CacheKey, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        // Fetch catalog feeds concurrently from TMDB
        var trendingMoviesTask = _tmdbService.GetTrendingMoviesAsync(cancellationToken);
        var trendingTvTask = _tmdbService.GetTrendingTvAsync(cancellationToken);
        var popularMoviesTask = _tmdbService.GetPopularMoviesAsync(cancellationToken);
        var topRatedMoviesTask = _tmdbService.GetTopRatedMoviesAsync(cancellationToken);
        var popularTvTask = _tmdbService.GetPopularTvAsync(cancellationToken);
        var upcomingMoviesTask = _tmdbService.GetUpcomingMoviesAsync(cancellationToken);

        await Task.WhenAll(
            trendingMoviesTask,
            trendingTvTask,
            popularMoviesTask,
            topRatedMoviesTask,
            popularTvTask,
            upcomingMoviesTask
        );

        var trendingMovies = await trendingMoviesTask;
        var trendingTv = await trendingTvTask;
        var popularMovies = await popularMoviesTask;
        var topRatedMovies = await topRatedMoviesTask;
        var popularTv = await popularTvTask;
        var upcomingMovies = await upcomingMoviesTask;

        // Choose the best candidate for Hero banner (first trending movie with backdrop)
        var heroCandidate = trendingMovies.FirstOrDefault(m => !string.IsNullOrEmpty(m.BackdropPath)) 
            ?? trendingMovies.FirstOrDefault()
            ?? new MediaCardDto(0, "movie", "Featured Title", null, null, 8.5, DateTime.UtcNow.Year.ToString(), new List<string> { "Action", "Adventure" });

        var heroDetails = await _tmdbService.GetHeroDetailsAsync(heroCandidate.TmdbId, heroCandidate.Type, cancellationToken);
        var hero = heroDetails ?? new HeroItemDto(
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
            new("trending-series", "Trending Series", "series", "/series?sort=trending", trendingTv),
            new("popular-movies", "Popular Movies", "movie", "/movies?sort=popular", popularMovies),
            new("top-rated-movies", "Top Rated Movies", "movie", "/movies?sort=top_rated", topRatedMovies),
            new("popular-series", "Popular Series", "series", "/series?sort=popular", popularTv),
            new("upcoming-movies", "Upcoming Movies", "movie", "/movies?sort=upcoming", upcomingMovies)
        };

        var feed = new HomeFeedDto(hero, rows);

        // Cache the home feed
        await _cacheService.SetAsync(CacheKey, feed, CacheDuration, cancellationToken);

        return feed;
    }
}
