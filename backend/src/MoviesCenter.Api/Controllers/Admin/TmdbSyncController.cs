using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Admin.Dtos;

namespace MoviesCenter.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/tmdb")]
public class TmdbSyncController : ControllerBase
{
    private readonly ITmdbSyncService _syncService;

    public TmdbSyncController(ITmdbSyncService syncService)
    {
        _syncService = syncService;
    }

    /// <summary>
    /// Trigger an on-demand TMDB catalog synchronization.
    /// </summary>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(TmdbSyncResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TmdbSyncResultDto>> TriggerSync(CancellationToken cancellationToken)
    {
        var result = await _syncService.RunFullSyncAsync("Admin", cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Check current or last TMDB synchronization status.
    /// </summary>
    [HttpGet("sync/status")]
    [ProducesResponseType(typeof(TmdbSyncStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TmdbSyncStatusDto>> GetSyncStatus(CancellationToken cancellationToken)
    {
        var status = await _syncService.GetSyncStatusAsync(cancellationToken);
        return Ok(status);
    }

    /// <summary>
    /// Import or refresh a single title directly from TMDB into the SQL database.
    /// </summary>
    [HttpPost("import")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportSingle([FromQuery] int tmdbId, [FromQuery] string type = "movie", CancellationToken cancellationToken = default)
    {
        if (tmdbId <= 0) return BadRequest(new { message = "Invalid TMDB ID" });

        var success = await _syncService.SyncSingleTitleAsync(tmdbId, type, cancellationToken);
        if (!success) return BadRequest(new { message = $"Could not import TMDB {type} with ID {tmdbId}" });

        return Ok(new { message = $"Successfully imported {type} {tmdbId} into catalog." });
    }
}
