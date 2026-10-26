using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.Catalog.Dtos;
using MoviesCenter.Application.Features.Catalog.Queries;
using MoviesCenter.Application.Features.Home.Dtos;
using MoviesCenter.Application.Features.Ratings.Commands;
using MoviesCenter.Application.Features.Ratings.Dtos;
using MoviesCenter.Application.Features.Ratings.Queries;

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

    /// <summary>
    /// Get playback streaming and embed sources for a title or TV episode.
    /// </summary>
    [HttpGet("{id:int}/sources")]
    [ProducesResponseType(typeof(List<MoviesCenter.Application.Features.Player.Dtos.TitleSourceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MoviesCenter.Application.Features.Player.Dtos.TitleSourceDto>>> GetTitleSources(
        int id,
        [FromQuery] int? season = null,
        [FromQuery] int? episode = null,
        CancellationToken cancellationToken = default)
    {
        var query = new MoviesCenter.Application.Features.Player.Queries.GetTitleSourcesQuery(id, season, episode);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get rating summary and distribution for a title, including the authenticated user's rating if logged in.
    /// </summary>
    [HttpGet("{id:int}/rating")]
    [ProducesResponseType(typeof(RatingDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RatingDto>> GetTitleRating(
        int id,
        CancellationToken cancellationToken = default)
    {
        Guid? userId = TryGetUserId(out var uid) ? uid : null;
        var query = new GetTitleRatingQuery(id, userId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Submit or update 1-5 star rating for a title.
    /// </summary>
    [HttpPost("{id:int}/rating")]
    [Authorize]
    [ProducesResponseType(typeof(RatingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RatingDto>> SubmitRating(
        int id,
        [FromBody] SubmitRatingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (request.Score < 1 || request.Score > 5)
        {
            return BadRequest(new { message = "Rating score must be between 1 and 5." });
        }

        try
        {
            var command = new SubmitRatingCommand(id, userId, request.Score);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove the authenticated user's rating for a title.
    /// </summary>
    [HttpDelete("{id:int}/rating")]
    [Authorize]
    [ProducesResponseType(typeof(RatingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RatingDto>> RemoveRating(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var command = new RemoveRatingCommand(id, userId);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(userIdStr, out userId);
    }
}
