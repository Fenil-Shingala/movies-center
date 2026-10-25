using MediatR;
using Microsoft.AspNetCore.Mvc;
using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.Catalog.Dtos;
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

    /// <summary>
    /// Get complete title details by ID (database ID or TMDB ID).
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TitleDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TitleDetailDto>> GetTitleDetail(
        int id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTitleDetailQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            return NotFound(new { message = $"Title with ID {id} not found." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get seasons and episodes for a TV series.
    /// </summary>
    [HttpGet("{id:int}/seasons")]
    [ProducesResponseType(typeof(List<SeasonDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SeasonDto>>> GetTitleSeasons(
        int id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTitleSeasonsQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get streaming/where-to-watch providers for a title.
    /// </summary>
    [HttpGet("{id:int}/where-to-watch")]
    [ProducesResponseType(typeof(List<WatchProviderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<WatchProviderDto>>> GetWhereToWatch(
        int id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTitleWhereToWatchQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
