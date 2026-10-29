using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesCenter.Application.Features.Reviews.Commands;
using MoviesCenter.Application.Features.Reviews.Dtos;
using MoviesCenter.Application.Features.Reviews.Queries;

namespace MoviesCenter.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Route("api")]
public class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReviewsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get paginated reviews for a title (sorted by popular or newest).
    /// </summary>
    [HttpGet("titles/{titleId:int}/reviews")]
    [ProducesResponseType(typeof(ReviewsListResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReviewsListResponseDto>> GetTitleReviews(
        int titleId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sort = "popular",
        CancellationToken cancellationToken = default)
    {
        Guid? userId = TryGetUserId(out var uid) ? uid : null;
        var query = new GetTitleReviewsQuery(titleId, page, pageSize, sort, userId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Post a new review for a title.
    /// </summary>
    [HttpPost("titles/{titleId:int}/reviews")]
    [Authorize]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewDto>> CreateReview(
        int titleId,
        [FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { message = "Review content is required." });
        }

        try
        {
            var command = new CreateReviewCommand(titleId, userId, request);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a review (author or admin only).
    /// </summary>
    [HttpDelete("reviews/{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteReview(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var isAdmin = User.IsInRole("Admin");
        var command = new DeleteReviewCommand(id, userId, isAdmin);
        var success = await _mediator.Send(command, cancellationToken);

        if (!success)
        {
            return NotFound(new { message = "Review not found or you are not authorized to delete it." });
        }

        return Ok(new { success = true, message = "Review deleted successfully." });
    }

    /// <summary>
    /// Toggle helpful upvote on a review.
    /// </summary>
    [HttpPost("reviews/{id:int}/like")]
    [Authorize]
    [ProducesResponseType(typeof(ToggleLikeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ToggleLikeResponseDto>> ToggleLike(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        try
        {
            var command = new ToggleReviewLikeCommand(id, userId);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out userId);
    }
}
