using MoviesCenter.Domain.Entities.Identity;

namespace MoviesCenter.Domain.Entities;

public class WatchHistoryItem
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int TitleId { get; set; }
    public int? EpisodeId { get; set; }
    public int? SeasonNumber { get; set; }
    public int? EpisodeNumber { get; set; }
    public int ProgressSeconds { get; set; }
    public int? DurationSeconds { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime WatchedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Title Title { get; set; } = null!;
    public Episode? Episode { get; set; }
}
