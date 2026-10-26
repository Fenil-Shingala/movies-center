using MoviesCenter.Domain.Entities.Identity;

namespace MoviesCenter.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles, out DateTime expiresAt);
    RefreshToken GenerateRefreshToken(string? ipAddress = null);
}
