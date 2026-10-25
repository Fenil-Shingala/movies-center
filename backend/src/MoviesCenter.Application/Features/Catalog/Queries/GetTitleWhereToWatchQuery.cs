using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Catalog.Dtos;

namespace MoviesCenter.Application.Features.Catalog.Queries;

public record GetTitleWhereToWatchQuery(int Id) : IRequest<List<WatchProviderDto>>;

public class GetTitleWhereToWatchQueryHandler : IRequestHandler<GetTitleWhereToWatchQuery, List<WatchProviderDto>>
{
    private readonly ICatalogQueryService _catalogService;

    public GetTitleWhereToWatchQueryHandler(ICatalogQueryService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<List<WatchProviderDto>> Handle(GetTitleWhereToWatchQuery request, CancellationToken cancellationToken)
    {
        return await _catalogService.GetTitleWhereToWatchAsync(request.Id, cancellationToken);
    }
}
