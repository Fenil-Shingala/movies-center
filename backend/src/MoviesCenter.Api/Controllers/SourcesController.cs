using MediatR;
using Microsoft.AspNetCore.Mvc;
using MoviesCenter.Application.Features.Player.Commands;
using MoviesCenter.Application.Features.Player.Dtos;

namespace MoviesCenter.Api.Controllers;

[ApiController]
[Route("api/v1/sources")]
public class SourcesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SourcesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Report an issue or broken stream with a player source.
    /// </summary>
    [HttpPost("{id:int}/report")]
    [ProducesResponseType(typeof(SourceReportResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SourceReportResponseDto>> ReportSource(
        int id,
        [FromBody] ReportSourceRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new ReportSourceCommand(
            SourceId: id,
            TitleId: request.TitleId,
            Reason: request.Reason,
            Details: request.Details,
            Season: request.Season,
            Episode: request.Episode
        );

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
