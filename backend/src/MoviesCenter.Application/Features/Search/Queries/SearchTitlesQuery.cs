using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.Home.Dtos;

namespace MoviesCenter.Application.Features.Search.Queries;

public record SearchTitlesQuery(
    string? Query,
    string? Type = null,
    int Page = 1,
    int PageSize = 24
) : IRequest<PagedResult<MediaCardDto>>;

public class SearchTitlesQueryHandler : IRequestHandler<SearchTitlesQuery, PagedResult<MediaCardDto>>
{
    private readonly ICatalogQueryService _catalogService;

    public SearchTitlesQueryHandler(ICatalogQueryService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<PagedResult<MediaCardDto>> Handle(SearchTitlesQuery request, CancellationToken cancellationToken)
    {
        return await _catalogService.SearchTitlesAsync(request, cancellationToken);
    }
}
