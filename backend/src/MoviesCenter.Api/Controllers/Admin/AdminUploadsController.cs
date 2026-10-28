using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Admin.Dtos;

namespace MoviesCenter.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/uploads")]
[Authorize(Roles = "Admin")]
public class AdminUploadsController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminUploadsController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>
    /// Generate a direct-upload endpoint for video assets (Bunny Stream / simulation).
    /// </summary>
    [HttpPost("create-url")]
    [ProducesResponseType(typeof(UploadUrlResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UploadUrlResponseDto>> CreateUploadUrl(
        [FromQuery] string fileName = "video.mp4",
        [FromQuery] string title = "Uploaded Video",
        CancellationToken cancellationToken = default)
    {
        var result = await _adminService.CreateDirectUploadUrlAsync(fileName, title, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Check processing and encoding status of an uploaded asset.
    /// </summary>
    [HttpGet("{assetId}/status")]
    [ProducesResponseType(typeof(UploadStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UploadStatusDto>> GetStatus(
        string assetId,
        CancellationToken cancellationToken = default)
    {
        var result = await _adminService.GetUploadStatusAsync(assetId, cancellationToken);
        return Ok(result);
    }
}
