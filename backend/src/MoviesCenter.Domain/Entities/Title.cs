using MoviesCenter.Domain.Common;

namespace MoviesCenter.Domain.Entities;

public class Title : BaseEntity
{
    public int TmdbId { get; set; }
    public string Type { get; set; } = "movie"; // movie, series
    public string TitleText { get; set; } = string.Empty;
    public string? OriginalTitle { get; set; }
    public string? Overview { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public int? RuntimeMinutes { get; set; }
    public decimal? ImdbRating { get; set; }
    public decimal? TmdbRating { get; set; }
    public string? ContentRating { get; set; }
    public string OriginalLanguage { get; set; } = "en";
    public string? Status { get; set; }
    public int? TotalSeasons { get; set; }
    public int? TotalEpisodes { get; set; }
    public bool IsFeatured { get; set; } = false;
    public long ViewCount { get; set; } = 0;

    public ICollection<TitleGenre> TitleGenres { get; set; } = new List<TitleGenre>();
    public ICollection<TitleSource> TitleSources { get; set; } = new List<TitleSource>();
}
