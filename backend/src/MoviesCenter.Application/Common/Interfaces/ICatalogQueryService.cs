using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.Catalog.Dtos;
using MoviesCenter.Application.Features.Catalog.Queries;
using MoviesCenter.Application.Features.Home.Dtos;

namespace MoviesCenter.Application.Common.Interfaces;

public interface ICatalogQueryService
{
    Task<HomeFeedDto> GetHomeFeedAsync(CancellationToken cancellationToken = default);
    Task<List<MediaCardDto>> GetTitlesByCategoryAsync(string category, string type, int limit = 20, CancellationToken cancellationToken = default);
    Task<HeroItemDto?> GetFeaturedHeroAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<MediaCardDto>> BrowseTitlesAsync(BrowseTitlesQuery query, CancellationToken cancellationToken = default);
    Task<List<GenreDto>> GetGenresAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<MediaCardDto>> SearchTitlesAsync(MoviesCenter.Application.Features.Search.Queries.SearchTitlesQuery query, CancellationToken cancellationToken = default);
    Task<List<MoviesCenter.Application.Features.Search.Dtos.SearchSuggestionDto>> GetSearchSuggestionsAsync(string query, int limit = 6, CancellationToken cancellationToken = default);
    Task<TitleDetailDto?> GetTitleDetailAsync(int id, CancellationToken cancellationToken = default);
    Task<List<SeasonDto>> GetTitleSeasonsAsync(int id, CancellationToken cancellationToken = default);
    Task<List<WatchProviderDto>> GetTitleWhereToWatchAsync(int id, CancellationToken cancellationToken = default);
    Task<List<MoviesCenter.Application.Features.Player.Dtos.TitleSourceDto>> GetTitleSourcesAsync(int titleId, int? season = null, int? episode = null, CancellationToken cancellationToken = default);
    Task<MoviesCenter.Application.Features.Player.Dtos.SourceReportResponseDto> ReportSourceAsync(int sourceId, int titleId, string reason, string? details = null, int? season = null, int? episode = null, CancellationToken cancellationToken = default);
    Task<List<MediaCardDto>> GetTitleRecommendationsAsync(int id, int limit = 12, CancellationToken cancellationToken = default);
    Task<List<MediaCardDto>> GetTop10Async(string type = "all", CancellationToken cancellationToken = default);
}
