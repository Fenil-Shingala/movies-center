using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.Admin.Dtos;

namespace MoviesCenter.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/sources")]
[Authorize(Roles = "Admin")]
public class AdminSourcesController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminSourcesController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>
    /// List streaming sources with filtering and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminSourceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminSourceDto>>> GetSources(
        [FromQuery] int? titleId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _adminService.GetSourcesAsync(titleId, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Add a new streaming source (custom embed or direct stream).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AdminSourceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminSourceDto>> CreateSource(
        [FromBody] CreateAdminSourceRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TitleId <= 0 || string.IsNullOrWhiteSpace(request.Url) || string.IsNullOrWhiteSpace(request.ProviderName))
        {
            return BadRequest(new { message = "TitleId, ProviderName, and Url are required." });
        }

        try
        {
            var result = await _adminService.CreateSourceAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing streaming source.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AdminSourceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminSourceDto>> UpdateSource(
        int id,
        [FromBody] UpdateAdminSourceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _adminService.UpdateSourceAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a streaming source.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSource(int id, CancellationToken cancellationToken)
    {
        var success = await _adminService.DeleteSourceAsync(id, cancellationToken);
        if (!success) return NotFound(new { message = $"Source #{id} not found." });
        return Ok(new { message = $"Source #{id} deleted successfully." });
    }
}
