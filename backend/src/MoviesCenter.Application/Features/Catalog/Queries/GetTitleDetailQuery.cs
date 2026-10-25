using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Catalog.Dtos;

namespace MoviesCenter.Application.Features.Catalog.Queries;

public record GetTitleDetailQuery(int Id) : IRequest<TitleDetailDto?>;

public class GetTitleDetailQueryHandler : IRequestHandler<GetTitleDetailQuery, TitleDetailDto?>
{
    private readonly ICatalogQueryService _catalogService;

    public GetTitleDetailQueryHandler(ICatalogQueryService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<TitleDetailDto?> Handle(GetTitleDetailQuery request, CancellationToken cancellationToken)
    {
        return await _catalogService.GetTitleDetailAsync(request.Id, cancellationToken);
    }
}
