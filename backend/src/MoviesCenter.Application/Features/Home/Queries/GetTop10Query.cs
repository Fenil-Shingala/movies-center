using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Home.Dtos;

namespace MoviesCenter.Application.Features.Home.Queries;

public sealed record GetTop10Query(string Type = "all") : IRequest<List<MediaCardDto>>;

public class GetTop10QueryHandler : IRequestHandler<GetTop10Query, List<MediaCardDto>>
{
    private readonly ICatalogQueryService _catalogQueryService;

    public GetTop10QueryHandler(ICatalogQueryService catalogQueryService)
    {
        _catalogQueryService = catalogQueryService;
    }

    public async Task<List<MediaCardDto>> Handle(GetTop10Query request, CancellationToken cancellationToken)
    {
        return await _catalogQueryService.GetTop10Async(request.Type, cancellationToken);
    }
}
