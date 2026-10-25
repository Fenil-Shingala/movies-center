using MoviesCenter.Domain.Common;

namespace MoviesCenter.Domain.Entities;

public class Episode : BaseEntity
{
    public int SeasonId { get; set; }
    public Season Season { get; set; } = null!;

    public int TmdbId { get; set; }
    public int EpisodeNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Overview { get; set; }
    public string? StillPath { get; set; }
    public DateTime? AirDate { get; set; }
    public int? RuntimeMinutes { get; set; }
}
