using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.Auth.Commands;
using MoviesCenter.Application.Features.Auth.Dtos;
using MoviesCenter.Application.Features.UserData.Commands;
using MoviesCenter.Application.Features.UserData.Dtos;
using MoviesCenter.Application.Features.UserData.Queries;

namespace MoviesCenter.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly IMediator _mediator;

    public MeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Explicitly merge guest watchlist and history items into the user's account.
    /// </summary>
    [HttpPost("import-guest-data")]
    [ProducesResponseType(typeof(GuestMergeResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GuestMergeResultDto>> ImportGuestData(
        [FromBody] ImportGuestDataRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        try
        {
            var result = await _mediator.Send(new ImportGuestDataCommand(userId, request), cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get the authenticated user's watchlist with optional filtering by type (movie/series).
    /// </summary>
    [HttpGet("watchlist")]
    [ProducesResponseType(typeof(PagedResult<WatchlistItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<WatchlistItemDto>>> GetWatchlist(
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _mediator.Send(new GetWatchlistQuery(userId, type, page, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Add a title to the user's watchlist.
    /// </summary>
    [HttpPost("watchlist")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddToWatchlist(
        [FromBody] AddToWatchlistRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        try
        {
            await _mediator.Send(new AddToWatchlistCommand(userId, request.TitleId), cancellationToken);
            return Ok(new { message = "Added to watchlist successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove a title from the user's watchlist.
    /// </summary>
    [HttpDelete("watchlist/{titleId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveFromWatchlist(
        int titleId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var removed = await _mediator.Send(new RemoveFromWatchlistCommand(userId, titleId), cancellationToken);
        return Ok(new { success = removed });
    }

    /// <summary>
    /// Check whether a title is in the user's watchlist.
    /// </summary>
    [HttpGet("watchlist/check/{titleId:int}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<bool>> CheckWatchlist(
        int titleId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var inWatchlist = await _mediator.Send(new CheckWatchlistStatusQuery(userId, titleId), cancellationToken);
        return Ok(new { inWatchlist });
    }

    /// <summary>
    /// Get the user's watch history / continue watching items.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(PagedResult<WatchHistoryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<WatchHistoryItemDto>>> GetWatchHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _mediator.Send(new GetWatchHistoryQuery(userId, page, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update progress for a movie or TV episode.
    /// </summary>
    [HttpPut("history")]
    [ProducesResponseType(typeof(WatchHistoryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WatchHistoryItemDto>> UpdateWatchProgress(
        [FromBody] UpdateWatchProgressRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        try
        {
            var result = await _mediator.Send(new UpdateWatchProgressCommand(userId, request), cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove an item from watch history.
    /// </summary>
    [HttpDelete("history/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveFromWatchHistory(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var removed = await _mediator.Send(new RemoveFromWatchHistoryCommand(userId, id), cancellationToken);
        return Ok(new { success = removed });
    }

    /// <summary>
    /// Clear all watch history items for the authenticated user.
    /// </summary>
    [HttpDelete("history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ClearWatchHistory(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        await _mediator.Send(new ClearWatchHistoryCommand(userId), cancellationToken);
        return Ok(new { message = "Watch history cleared successfully." });
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(userIdStr, out userId);
    }
}
