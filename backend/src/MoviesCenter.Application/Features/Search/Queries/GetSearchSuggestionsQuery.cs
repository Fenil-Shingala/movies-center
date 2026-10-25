using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Search.Dtos;

namespace MoviesCenter.Application.Features.Search.Queries;

public record GetSearchSuggestionsQuery(
    string Query,
    int Limit = 6
) : IRequest<List<SearchSuggestionDto>>;

public class GetSearchSuggestionsQueryHandler : IRequestHandler<GetSearchSuggestionsQuery, List<SearchSuggestionDto>>
{
    private readonly ICatalogQueryService _catalogService;

    public GetSearchSuggestionsQueryHandler(ICatalogQueryService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<List<SearchSuggestionDto>> Handle(GetSearchSuggestionsQuery request, CancellationToken cancellationToken)
    {
        return await _catalogService.GetSearchSuggestionsAsync(request.Query, request.Limit, cancellationToken);
    }
}
