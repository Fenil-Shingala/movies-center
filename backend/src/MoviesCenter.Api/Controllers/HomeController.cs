using MediatR;
using Microsoft.AspNetCore.Mvc;
using MoviesCenter.Application.Features.Home.Dtos;
using MoviesCenter.Application.Features.Home.Queries;

namespace MoviesCenter.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Route("api/[controller]")]
public class HomeController : ControllerBase
{
    private readonly IMediator _mediator;

    public HomeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieves curated home feed containing hero spotlight and content rows.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(HomeFeedDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<HomeFeedDto>> GetHomeFeed(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetHomeFeedQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves top 10 trending ranked titles for home page showcase.
    /// </summary>
    [HttpGet("top10")]
    [ProducesResponseType(typeof(List<MediaCardDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MediaCardDto>>> GetTop10([FromQuery] string type = "all", CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetTop10Query(type), cancellationToken);
        return Ok(result);
    }
}
