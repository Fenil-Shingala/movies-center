using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Catalog.Dtos;

namespace MoviesCenter.Application.Features.Catalog.Queries;

public record GetGenresQuery : IRequest<List<GenreDto>>;

public class GetGenresQueryHandler : IRequestHandler<GetGenresQuery, List<GenreDto>>
{
    private readonly ICatalogQueryService _catalogQueryService;

    public GetGenresQueryHandler(ICatalogQueryService catalogQueryService)
    {
        _catalogQueryService = catalogQueryService;
    }

    public async Task<List<GenreDto>> Handle(GetGenresQuery request, CancellationToken cancellationToken)
    {
        return await _catalogQueryService.GetGenresAsync(cancellationToken);
    }
}
