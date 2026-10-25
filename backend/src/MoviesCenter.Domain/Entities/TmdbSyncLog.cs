using MoviesCenter.Domain.Common;

namespace MoviesCenter.Domain.Entities;

public class TmdbSyncLog : BaseEntity
{
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public string Status { get; set; } = "Running"; // Running, Completed, Failed
    public int ItemsSynced { get; set; }
    public int Errors { get; set; }
    public string TriggeredBy { get; set; } = "HangfireJob"; // Admin, HangfireJob
    public string? ErrorMessage { get; set; }
}
