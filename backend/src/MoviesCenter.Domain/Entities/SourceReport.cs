namespace MoviesCenter.Domain.Entities;

public class SourceReport
{
    public int Id { get; set; }
    public int SourceId { get; set; }
    public int TitleId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }
    public int? Season { get; set; }
    public int? Episode { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Reviewed, Resolved, Dismissed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Title? Title { get; set; }
    public TitleSource? Source { get; set; }
}
