using MoviesCenter.Application.Features.Home.Dtos;

namespace MoviesCenter.Application.Common.Interfaces;

public interface ITmdbService
{
    Task<List<MediaCardDto>> GetTrendingMoviesAsync(CancellationToken cancellationToken = default);
    Task<List<MediaCardDto>> GetTrendingTvAsync(CancellationToken cancellationToken = default);
    Task<List<MediaCardDto>> GetPopularMoviesAsync(CancellationToken cancellationToken = default);
    Task<List<MediaCardDto>> GetTopRatedMoviesAsync(CancellationToken cancellationToken = default);
    Task<List<MediaCardDto>> GetPopularTvAsync(CancellationToken cancellationToken = default);
    Task<List<MediaCardDto>> GetUpcomingMoviesAsync(CancellationToken cancellationToken = default);
    Task<HeroItemDto?> GetHeroDetailsAsync(int tmdbId, string type, CancellationToken cancellationToken = default);
    Task<Dictionary<int, string>> GetGenreMapAsync(CancellationToken cancellationToken = default);
}
