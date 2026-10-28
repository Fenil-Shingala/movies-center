namespace MoviesCenter.Domain.Entities;

using MoviesCenter.Domain.Entities.Identity;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }

    /// <summary>
    /// Type of notification: NewEpisode, ContinueWatchingReminder, SystemAlert, NewRelease, etc.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// JSON payload with structured references (e.g. { "titleId": 12, "episodeId": 34, "actionUrl": "/watch/series/12" })
    /// </summary>
    public string? Data { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAt { get; set; }
}
