using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.Admin.Dtos;

namespace MoviesCenter.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/reports")]
[Authorize(Roles = "Admin")]
public class AdminReportsController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminReportsController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>
    /// View broken-source reports submitted by users.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminReportDto>>> GetReports(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _adminService.GetReportsAsync(status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update report status (e.g. Reviewed, Resolved, Dismissed).
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateReportStatus(
        int id,
        [FromBody] UpdateReportStatusRequest request,
        CancellationToken cancellationToken)
    {
        var success = await _adminService.UpdateReportStatusAsync(id, request.Status, cancellationToken);
        if (!success) return NotFound(new { message = $"Report #{id} not found." });
        return Ok(new { message = $"Report #{id} marked as {request.Status}." });
    }
}
