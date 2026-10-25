using MoviesCenter.Domain.Common;

namespace MoviesCenter.Domain.Entities;

public class Season : BaseEntity
{
    public int TitleId { get; set; }
    public Title Title { get; set; } = null!;

    public int TmdbId { get; set; }
    public int SeasonNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Overview { get; set; }
    public string? PosterPath { get; set; }
    public DateTime? AirDate { get; set; }
    public int EpisodeCount { get; set; }

    public ICollection<Episode> Episodes { get; set; } = new List<Episode>();
}
