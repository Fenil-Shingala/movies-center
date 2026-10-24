using Microsoft.AspNetCore.Identity;

namespace MoviesCenter.Domain.Entities.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
