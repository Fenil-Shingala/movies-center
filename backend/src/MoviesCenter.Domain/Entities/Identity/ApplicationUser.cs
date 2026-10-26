using Microsoft.AspNetCore.Identity;

namespace MoviesCenter.Domain.Entities.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<WatchlistItem> WatchlistItems { get; set; } = [];
    public ICollection<WatchHistoryItem> WatchHistory { get; set; } = [];
    public ICollection<Rating> Ratings { get; set; } = [];
}
