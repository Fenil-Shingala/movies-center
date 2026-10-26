using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Auth.Dtos;
using MoviesCenter.Domain.Constants;
using MoviesCenter.Domain.Entities;
using MoviesCenter.Domain.Entities.Identity;
using MoviesCenter.Infrastructure.Data;

namespace MoviesCenter.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponseDto> RegisterAsync(
        RegisterRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("An account with this email address already exists.");
        }

        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? request.Email.Split('@')[0]
            : request.DisplayName.Trim();

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = displayName,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Registration failed: {errors}");
        }

        await _userManager.AddToRoleAsync(user, AppConstants.Roles.User);

        // Generate Refresh Token
        var refreshToken = _jwtTokenService.GenerateRefreshToken(ipAddress);
        refreshToken.UserId = user.Id;
        _dbContext.RefreshTokens.Add(refreshToken);

        // Optional guest data merge
        if (request.GuestData != null)
        {
            await MergeGuestDataInternalAsync(user.Id, request.GuestData, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles, out var expiresAt);

        var userProfile = new UserProfileDto(
            user.Id,
            user.Email!,
            user.DisplayName ?? user.Email!,
            user.AvatarUrl,
            roles.ToList()
        );

        return new AuthResponseDto(accessToken, expiresAt, userProfile, refreshToken.Token);
    }

    public async Task<AuthResponseDto> LoginAsync(
        LoginRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // Generate Refresh Token
        var refreshToken = _jwtTokenService.GenerateRefreshToken(ipAddress);
        refreshToken.UserId = user.Id;
        _dbContext.RefreshTokens.Add(refreshToken);

        // Optional guest data merge
        if (request.GuestData != null)
        {
            await MergeGuestDataInternalAsync(user.Id, request.GuestData, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles, out var expiresAt);

        var userProfile = new UserProfileDto(
            user.Id,
            user.Email!,
            user.DisplayName ?? user.Email!,
            user.AvatarUrl,
            roles.ToList()
        );

        return new AuthResponseDto(accessToken, expiresAt, userProfile, refreshToken.Token);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(
        string refreshTokenString,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var token = await _dbContext.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == refreshTokenString, cancellationToken);

        if (token == null || !token.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        // Rotate token
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken(ipAddress);
        newRefreshToken.UserId = token.UserId;

        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReplacedByToken = newRefreshToken.Token;

        _dbContext.RefreshTokens.Add(newRefreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var user = token.User;
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles, out var expiresAt);

        var userProfile = new UserProfileDto(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName ?? user.Email ?? string.Empty,
            user.AvatarUrl,
            roles.ToList()
        );

        return new AuthResponseDto(accessToken, expiresAt, userProfile, newRefreshToken.Token);
    }

    public async Task<bool> RevokeTokenAsync(
        string refreshTokenString,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var token = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == refreshTokenString, cancellationToken);

        if (token == null || !token.IsActive)
        {
            return false;
        }

        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<UserProfileDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new UserProfileDto(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName ?? user.Email ?? string.Empty,
            user.AvatarUrl,
            roles.ToList()
        );
    }

    public async Task<GuestMergeResultDto> ImportGuestDataAsync(
        Guid userId,
        ImportGuestDataRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        var guestData = new GuestDataDto(request.WatchlistTitleIds, request.History);
        var (watchlistCount, historyCount) = await MergeGuestDataInternalAsync(userId, guestData, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new GuestMergeResultDto(
            watchlistCount,
            historyCount,
            $"Successfully merged {watchlistCount} watchlist items and {historyCount} history items."
        );
    }

    private async Task<(int WatchlistCount, int HistoryCount)> MergeGuestDataInternalAsync(
        Guid userId,
        GuestDataDto guestData,
        CancellationToken cancellationToken)
    {
        int watchlistAdded = 0;
        int historyMerged = 0;

        // 1. Merge Watchlist
        if (guestData.WatchlistTitleIds != null && guestData.WatchlistTitleIds.Count > 0)
        {
            var validTitleIds = await _dbContext.Titles
                .Where(t => guestData.WatchlistTitleIds.Contains(t.Id))
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);

            var existingIds = await _dbContext.WatchlistItems
                .Where(w => w.UserId == userId)
                .Select(w => w.TitleId)
                .ToListAsync(cancellationToken);

            var existingSet = new HashSet<int>(existingIds);

            foreach (var titleId in validTitleIds)
            {
                if (!existingSet.Contains(titleId))
                {
                    _dbContext.WatchlistItems.Add(new WatchlistItem
                    {
                        UserId = userId,
                        TitleId = titleId,
                        AddedAt = DateTime.UtcNow
                    });
                    existingSet.Add(titleId);
                    watchlistAdded++;
                }
            }
        }

        // 2. Merge History
        if (guestData.History != null && guestData.History.Count > 0)
        {
            var titleIds = guestData.History.Select(h => h.TitleId).Distinct().ToList();
            var validTitleIds = await _dbContext.Titles
                .Where(t => titleIds.Contains(t.Id))
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);

            var validTitleSet = new HashSet<int>(validTitleIds);

            var existingHistory = await _dbContext.WatchHistoryItems
                .Where(h => h.UserId == userId && titleIds.Contains(h.TitleId))
                .ToListAsync(cancellationToken);

            foreach (var item in guestData.History)
            {
                if (!validTitleSet.Contains(item.TitleId))
                {
                    continue;
                }

                var match = existingHistory.FirstOrDefault(h =>
                    h.TitleId == item.TitleId &&
                    h.EpisodeId == item.EpisodeId);

                var isCompleted = item.DurationSeconds.HasValue && item.DurationSeconds > 0 &&
                    item.ProgressSeconds >= (int)(item.DurationSeconds.Value * 0.9);

                if (match != null)
                {
                    if (item.ProgressSeconds > match.ProgressSeconds)
                    {
                        match.ProgressSeconds = item.ProgressSeconds;
                    }
                    if (item.DurationSeconds.HasValue && item.DurationSeconds > 0)
                    {
                        match.DurationSeconds = item.DurationSeconds;
                    }
                    if (item.WatchedAt.HasValue && item.WatchedAt > match.WatchedAt)
                    {
                        match.WatchedAt = item.WatchedAt.Value;
                    }
                    if (isCompleted)
                    {
                        match.IsCompleted = true;
                    }
                    historyMerged++;
                }
                else
                {
                    var newHistory = new WatchHistoryItem
                    {
                        UserId = userId,
                        TitleId = item.TitleId,
                        EpisodeId = item.EpisodeId,
                        SeasonNumber = item.SeasonNumber,
                        EpisodeNumber = item.EpisodeNumber,
                        ProgressSeconds = item.ProgressSeconds,
                        DurationSeconds = item.DurationSeconds,
                        IsCompleted = isCompleted,
                        WatchedAt = item.WatchedAt ?? DateTime.UtcNow
                    };
                    _dbContext.WatchHistoryItems.Add(newHistory);
                    existingHistory.Add(newHistory);
                    historyMerged++;
                }
            }
        }

        return (watchlistAdded, historyMerged);
    }
}
