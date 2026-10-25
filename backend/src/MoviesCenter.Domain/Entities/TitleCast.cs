using MoviesCenter.Domain.Common;

namespace MoviesCenter.Domain.Entities;

public class TitleCast : BaseEntity
{
    public int TitleId { get; set; }
    public Title Title { get; set; } = null!;

    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public string Character { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
