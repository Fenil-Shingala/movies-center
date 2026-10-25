using MoviesCenter.Domain.Common;

namespace MoviesCenter.Domain.Entities;

public class Video : BaseEntity
{
    public int TitleId { get; set; }
    public Title Title { get; set; } = null!;

    public string TmdbKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Site { get; set; } = "YouTube";
}
