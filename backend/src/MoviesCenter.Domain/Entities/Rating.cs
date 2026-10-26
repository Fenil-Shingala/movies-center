using MoviesCenter.Domain.Entities.Identity;

namespace MoviesCenter.Domain.Entities;

public class Rating
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int TitleId { get; set; }
    public int Score { get; set; } // 1..5 stars
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Title Title { get; set; } = null!;
}
