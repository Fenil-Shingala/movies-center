using MoviesCenter.Domain.Common;

namespace MoviesCenter.Domain.Entities;

public class SourceProvider : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // ThirdPartyEmbed, Upload
    public string? BaseUrl { get; set; }
    public string? EmbedUrlTemplate { get; set; } // e.g., https://vidsrc.to/embed/movie/{tmdbId}
    public int Priority { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    public ICollection<TitleSource> TitleSources { get; set; } = new List<TitleSource>();
}
