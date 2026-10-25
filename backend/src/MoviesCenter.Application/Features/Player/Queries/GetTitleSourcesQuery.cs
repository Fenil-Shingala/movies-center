using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Player.Dtos;

namespace MoviesCenter.Application.Features.Player.Queries;

public record GetTitleSourcesQuery(
    int TitleId,
    int? Season = null,
    int? Episode = null
) : IRequest<List<TitleSourceDto>>;

public class GetTitleSourcesQueryHandler : IRequestHandler<GetTitleSourcesQuery, List<TitleSourceDto>>
{
    private readonly ICatalogQueryService _catalogService;

    public GetTitleSourcesQueryHandler(ICatalogQueryService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<List<TitleSourceDto>> Handle(GetTitleSourcesQuery request, CancellationToken cancellationToken)
    {
        return await _catalogService.GetTitleSourcesAsync(request.TitleId, request.Season, request.Episode, cancellationToken);
    }
}
