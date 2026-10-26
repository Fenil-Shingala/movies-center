using MoviesCenter.Application.Features.Auth.Dtos;

namespace MoviesCenter.Application.Common.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequest request, string? ipAddress = null, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> LoginAsync(LoginRequest request, string? ipAddress = null, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, string? ipAddress = null, CancellationToken cancellationToken = default);
    Task<bool> RevokeTokenAsync(string refreshToken, string? ipAddress = null, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<GuestMergeResultDto> ImportGuestDataAsync(Guid userId, ImportGuestDataRequest request, CancellationToken cancellationToken = default);
}
