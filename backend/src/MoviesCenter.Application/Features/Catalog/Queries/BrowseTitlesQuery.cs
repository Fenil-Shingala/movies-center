using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.Home.Dtos;

namespace MoviesCenter.Application.Features.Catalog.Queries;

public record BrowseTitlesQuery(
    string Type = "movie",
    int? GenreId = null,
    int? Year = null,
    string? Language = null,
    string? Sort = "popularity.desc",
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<MediaCardDto>>;

public class BrowseTitlesQueryHandler : IRequestHandler<BrowseTitlesQuery, PagedResult<MediaCardDto>>
{
    private readonly ICatalogQueryService _catalogQueryService;

    public BrowseTitlesQueryHandler(ICatalogQueryService catalogQueryService)
    {
        _catalogQueryService = catalogQueryService;
    }

    public async Task<PagedResult<MediaCardDto>> Handle(BrowseTitlesQuery request, CancellationToken cancellationToken)
    {
        return await _catalogQueryService.BrowseTitlesAsync(request, cancellationToken);
    }
}
