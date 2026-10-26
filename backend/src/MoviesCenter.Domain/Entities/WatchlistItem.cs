using MoviesCenter.Domain.Entities.Identity;

namespace MoviesCenter.Domain.Entities;

public class WatchlistItem
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int TitleId { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Title Title { get; set; } = null!;
}
