using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Admin.Dtos;

namespace MoviesCenter.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/tmdb")]
[Authorize(Roles = "Admin")]
public class TmdbSyncController : ControllerBase
{
    private readonly ITmdbSyncService _syncService;
    private readonly ITmdbService _tmdbService;

    public TmdbSyncController(ITmdbSyncService syncService, ITmdbService tmdbService)
    {
        _syncService = syncService;
        _tmdbService = tmdbService;
    }

    /// <summary>
    /// Search TMDB directly to find movies or series to import.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<TmdbSearchResultItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TmdbSearchResultItemDto>>> SearchTmdb(
        [FromQuery] string q,
        [FromQuery] string? type = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(new List<TmdbSearchResultItemDto>());
        }

        var results = await _tmdbService.SearchTmdbAsync(q, type, cancellationToken);
        return Ok(results);
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
    public async Task<IActionResult> ImportSingle(
        [FromBody] ImportTmdbTitleRequest? body = null,
        [FromQuery] int tmdbId = 0,
        [FromQuery] string type = "movie",
        CancellationToken cancellationToken = default)
    {
        var targetId = body?.TmdbId > 0 ? body.TmdbId : tmdbId;
        var targetType = !string.IsNullOrEmpty(body?.Type) ? body.Type : type;

        if (targetId <= 0) return BadRequest(new { message = "Invalid TMDB ID" });

        var success = await _syncService.SyncSingleTitleAsync(targetId, targetType, cancellationToken);
        if (!success) return BadRequest(new { message = $"Could not import TMDB {targetType} with ID {targetId}" });

        return Ok(new { message = $"Successfully imported {targetType} {targetId} into catalog." });
    }
}
