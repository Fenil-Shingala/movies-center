namespace MoviesCenter.Domain.Entities;

using MoviesCenter.Domain.Entities.Identity;

public class NotificationSettings
{
    public int Id { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public bool NewEpisodeInApp { get; set; } = true;
    public bool NewEpisodeEmail { get; set; } = true;
    public bool NewEpisodePush { get; set; } = false;

    public bool ContinueWatchingInApp { get; set; } = true;
    public bool ContinueWatchingEmail { get; set; } = false;
    public bool ContinueWatchingPush { get; set; } = false;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
