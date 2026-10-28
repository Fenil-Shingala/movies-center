using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.Home.Dtos;

namespace MoviesCenter.Application.Common.Interfaces;

public interface ITmdbService
{
    // Legacy / direct methods
    Task<List<MediaCardDto>> GetTrendingMoviesAsync(CancellationToken cancellationToken = default);
    Task<List<MediaCardDto>> GetTrendingTvAsync(CancellationToken cancellationToken = default);
    Task<List<MediaCardDto>> GetPopularMoviesAsync(CancellationToken cancellationToken = default);
    Task<List<MediaCardDto>> GetTopRatedMoviesAsync(CancellationToken cancellationToken = default);
    Task<List<MediaCardDto>> GetPopularTvAsync(CancellationToken cancellationToken = default);
    Task<List<MediaCardDto>> GetUpcomingMoviesAsync(CancellationToken cancellationToken = default);
    Task<HeroItemDto?> GetHeroDetailsAsync(int tmdbId, string type, CancellationToken cancellationToken = default);
    Task<Dictionary<int, string>> GetGenreMapAsync(CancellationToken cancellationToken = default);

    // Sync methods
    Task<List<TmdbItemSummary>> GetListEndpointAsync(string endpoint, string type, CancellationToken cancellationToken = default);
    Task<TmdbFullDetail?> GetFullDetailsAsync(int tmdbId, string type, CancellationToken cancellationToken = default);
    Task<List<TmdbEpisodeSummary>> GetSeasonEpisodesAsync(int tmdbId, int seasonNumber, CancellationToken cancellationToken = default);
    Task<List<MoviesCenter.Application.Features.Admin.Dtos.TmdbSearchResultItemDto>> SearchTmdbAsync(string query, string? type = null, CancellationToken cancellationToken = default);
}
