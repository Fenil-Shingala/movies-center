using MediatR;
using Microsoft.AspNetCore.Mvc;
using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.Catalog.Queries;
using MoviesCenter.Application.Features.Home.Dtos;

namespace MoviesCenter.Api.Controllers;

[ApiController]
[Route("api/v1/titles")]
public class TitlesController : ControllerBase
{
    private readonly IMediator _mediator;

    public TitlesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Browse movies or series with filtering, sorting, and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MediaCardDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MediaCardDto>>> BrowseTitles(
        [FromQuery] string type = "movie",
        [FromQuery] int? genre = null,
        [FromQuery] int? year = null,
        [FromQuery] string? language = null,
        [FromQuery] string? sort = "popularity.desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new BrowseTitlesQuery(
            Type: type,
            GenreId: genre,
            Year: year,
            Language: language,
            Sort: sort,
            Page: page,
            PageSize: pageSize
        );

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
