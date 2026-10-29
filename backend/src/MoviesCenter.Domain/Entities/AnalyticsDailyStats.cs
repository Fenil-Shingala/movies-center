namespace MoviesCenter.Domain.Entities;

public class AnalyticsDailyStats
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string MetricName { get; set; } = string.Empty; // e.g. "plays", "signups", "searches", "empty_searches", "source_fails"
    public long MetricValue { get; set; }
    public string? Dimension { get; set; } // e.g. titleId, query term, provider name
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
