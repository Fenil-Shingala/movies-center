using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Home.Dtos;

namespace MoviesCenter.Application.Features.Home.Queries;

public record GetHomeFeedQuery : IRequest<HomeFeedDto>;

public class GetHomeFeedQueryHandler : IRequestHandler<GetHomeFeedQuery, HomeFeedDto>
{
    private readonly ICatalogQueryService _catalogQueryService;

    public GetHomeFeedQueryHandler(ICatalogQueryService catalogQueryService)
    {
        _catalogQueryService = catalogQueryService;
    }

    public async Task<HomeFeedDto> Handle(GetHomeFeedQuery request, CancellationToken cancellationToken)
    {
        return await _catalogQueryService.GetHomeFeedAsync(cancellationToken);
    }
}
