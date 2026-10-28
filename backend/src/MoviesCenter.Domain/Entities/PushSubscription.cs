namespace MoviesCenter.Domain.Entities;

using MoviesCenter.Domain.Entities.Identity;

public class PushSubscription
{
    public int Id { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string Endpoint { get; set; } = string.Empty;

    public string P256dh { get; set; } = string.Empty;

    public string Auth { get; set; } = string.Empty;

    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
