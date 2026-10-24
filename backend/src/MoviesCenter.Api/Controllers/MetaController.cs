using Microsoft.AspNetCore.Mvc;
using MoviesCenter.Domain.Constants;

namespace MoviesCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetaController : ControllerBase
{
    [HttpGet("info")]
    public IActionResult GetAppInfo()
    {
        return Ok(new
        {
            AppName = AppConstants.AppName,
            Version = AppConstants.AppVersion,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
            Providers = new[]
            {
                new { Name = "VidSrc", Type = AppConstants.SourceProviderTypes.ThirdPartyEmbed, Slug = AppConstants.Providers.VidSrc },
                new { Name = "Bunny Stream", Type = AppConstants.SourceProviderTypes.Upload, Slug = AppConstants.Providers.BunnyStream }
            }
        });
    }
}
