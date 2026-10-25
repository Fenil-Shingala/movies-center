using MoviesCenter.Domain.Common;

namespace MoviesCenter.Domain.Entities;

public class Provider : BaseEntity
{
    public int TmdbId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LogoPath { get; set; }

    public ICollection<TitleProvider> TitleProviders { get; set; } = new List<TitleProvider>();
}
