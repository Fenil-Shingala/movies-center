using MediatR;
using Microsoft.AspNetCore.Mvc;
using MoviesCenter.Application.Features.Analytics.Commands;
using MoviesCenter.Application.Features.Analytics.Dtos;

namespace MoviesCenter.Api.Controllers;

[ApiController]
[Route("api/v1/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AnalyticsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Ingest a batch of anonymous, consent-based telemetry events.
    /// </summary>
    [HttpPost("events")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IngestEvents(
        [FromBody] IngestEventsRequest request,
        CancellationToken cancellationToken)
    {
        if (request?.Events == null || request.Events.Count == 0)
        {
            return Ok(new { success = true, ingested = 0 });
        }

        if (request.Events.Count > 50)
        {
            return BadRequest(new { message = "Batch size cannot exceed 50 events." });
        }

        await _mediator.Send(new IngestEventsCommand(request.Events), cancellationToken);
        return Ok(new { success = true, ingested = request.Events.Count });
    }
}
