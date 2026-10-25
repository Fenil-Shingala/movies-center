using MediatR;
using Microsoft.AspNetCore.Mvc;
using MoviesCenter.Application.Features.Catalog.Dtos;
using MoviesCenter.Application.Features.Catalog.Queries;

namespace MoviesCenter.Api.Controllers;

[ApiController]
[Route("api/v1/genres")]
public class GenresController : ControllerBase
{
    private readonly IMediator _mediator;

    public GenresController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieve all genres for catalog browsing and filtering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<GenreDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GenreDto>>> GetGenres(CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetGenresQuery(), cancellationToken);
        return Ok(result);
    }
}
