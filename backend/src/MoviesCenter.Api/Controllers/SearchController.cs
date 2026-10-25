using MediatR;
using Microsoft.AspNetCore.Mvc;
using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.Home.Dtos;
using MoviesCenter.Application.Features.Search.Dtos;
using MoviesCenter.Application.Features.Search.Queries;

namespace MoviesCenter.Api.Controllers;

[ApiController]
[Route("api/v1/search")]
public class SearchController : ControllerBase
{
    private readonly IMediator _mediator;

    public SearchController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Search titles with optional type filtering and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MediaCardDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MediaCardDto>>> Search(
        [FromQuery] string? q = null,
        [FromQuery] string? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchTitlesQuery(
            Query: q,
            Type: type,
            Page: page,
            PageSize: pageSize
        );

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Fast debounced autocomplete search suggestions.
    /// </summary>
    [HttpGet("suggest")]
    [ProducesResponseType(typeof(List<SearchSuggestionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SearchSuggestionDto>>> Suggest(
        [FromQuery] string q,
        [FromQuery] int limit = 6,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSearchSuggestionsQuery(Query: q ?? string.Empty, Limit: limit);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
