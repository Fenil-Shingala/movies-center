using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.Admin.Dtos;

namespace MoviesCenter.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "Admin")]
public class AdminTitlesController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminTitlesController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>
    /// Get aggregated statistics for the admin dashboard.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(AdminStatsOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminStatsOverviewDto>> GetStats(CancellationToken cancellationToken)
    {
        var stats = await _adminService.GetStatsOverviewAsync(cancellationToken);
        return Ok(stats);
    }

    /// <summary>
    /// Search and list catalog titles with admin metadata and source counts.
    /// </summary>
    [HttpGet("titles")]
    [ProducesResponseType(typeof(PagedResult<AdminTitleListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminTitleListItemDto>>> GetTitles(
        [FromQuery] string? q = null,
        [FromQuery] string? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _adminService.GetTitlesAsync(q, type, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Toggle featured status for a title.
    /// </summary>
    [HttpPost("titles/{id:int}/toggle-featured")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleFeatured(int id, CancellationToken cancellationToken)
    {
        var success = await _adminService.ToggleTitleFeaturedAsync(id, cancellationToken);
        if (!success) return NotFound(new { message = $"Title #{id} not found." });
        return Ok(new { message = $"Title #{id} featured status toggled." });
    }

    /// <summary>
    /// Toggle active / visible status for a title.
    /// </summary>
    [HttpPost("titles/{id:int}/toggle-active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
    {
        var success = await _adminService.ToggleTitleActiveAsync(id, cancellationToken);
        if (!success) return NotFound(new { message = $"Title #{id} not found." });
        return Ok(new { message = $"Title #{id} active status toggled." });
    }
}
