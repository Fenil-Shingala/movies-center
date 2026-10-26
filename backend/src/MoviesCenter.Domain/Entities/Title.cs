using MoviesCenter.Domain.Common;

namespace MoviesCenter.Domain.Entities;

public class Title : BaseEntity
{
    public int TmdbId { get; set; }
    public string Type { get; set; } = "movie"; // movie, series
    public string TitleText { get; set; } = string.Empty;
    public string? OriginalTitle { get; set; }
    public string? Overview { get; set; }
    public string? Tagline { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public DateTime? FirstAirDate { get; set; }
    public int? RuntimeMinutes { get; set; }
    public decimal? ImdbRating { get; set; }
    public decimal? TmdbRating { get; set; }
    public int VoteCount { get; set; } = 0;
    public double Popularity { get; set; } = 0;
    public string? ContentRating { get; set; }
    public string OriginalLanguage { get; set; } = "en";
    public string? Status { get; set; }
    public int? TotalSeasons { get; set; }
    public int? TotalEpisodes { get; set; }
    public bool IsFeatured { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public long ViewCount { get; set; } = 0;
    public string? TmdbListCategory { get; set; } // comma separated e.g. "trending,popular,top_rated"
    public DateTimeOffset? LastTmdbSyncAt { get; set; }

    public ICollection<TitleGenre> TitleGenres { get; set; } = new List<TitleGenre>();
    public ICollection<TitleSource> TitleSources { get; set; } = new List<TitleSource>();
    public ICollection<Season> Seasons { get; set; } = new List<Season>();
    public ICollection<TitleCast> TitleCasts { get; set; } = new List<TitleCast>();
    public ICollection<Video> Videos { get; set; } = new List<Video>();
    public ICollection<TitleProvider> TitleProviders { get; set; } = new List<TitleProvider>();
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
}
