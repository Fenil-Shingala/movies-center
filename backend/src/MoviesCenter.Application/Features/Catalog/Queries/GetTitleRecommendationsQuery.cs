using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Home.Dtos;

namespace MoviesCenter.Application.Features.Catalog.Queries;

public sealed record GetTitleRecommendationsQuery(int TitleId, int Limit = 12) : IRequest<List<MediaCardDto>>;

public class GetTitleRecommendationsQueryHandler : IRequestHandler<GetTitleRecommendationsQuery, List<MediaCardDto>>
{
    private readonly ICatalogQueryService _catalogService;

    public GetTitleRecommendationsQueryHandler(ICatalogQueryService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<List<MediaCardDto>> Handle(GetTitleRecommendationsQuery request, CancellationToken cancellationToken)
    {
        return await _catalogService.GetTitleRecommendationsAsync(request.TitleId, request.Limit, cancellationToken);
    }
}
