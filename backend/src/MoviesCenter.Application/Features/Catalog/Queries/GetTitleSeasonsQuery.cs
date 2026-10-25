using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Catalog.Dtos;

namespace MoviesCenter.Application.Features.Catalog.Queries;

public record GetTitleSeasonsQuery(int Id) : IRequest<List<SeasonDto>>;

public class GetTitleSeasonsQueryHandler : IRequestHandler<GetTitleSeasonsQuery, List<SeasonDto>>
{
    private readonly ICatalogQueryService _catalogService;

    public GetTitleSeasonsQueryHandler(ICatalogQueryService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<List<SeasonDto>> Handle(GetTitleSeasonsQuery request, CancellationToken cancellationToken)
    {
        return await _catalogService.GetTitleSeasonsAsync(request.Id, cancellationToken);
    }
}
