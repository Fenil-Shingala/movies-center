using MoviesCenter.Domain.Common;

namespace MoviesCenter.Domain.Entities;

public class Person : BaseEntity
{
    public int TmdbId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ProfilePath { get; set; }

    public ICollection<TitleCast> TitleCasts { get; set; } = new List<TitleCast>();
}
