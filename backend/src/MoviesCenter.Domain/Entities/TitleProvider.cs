using MoviesCenter.Domain.Common;

namespace MoviesCenter.Domain.Entities;

public class TitleProvider : BaseEntity
{
    public int TitleId { get; set; }
    public Title Title { get; set; } = null!;

    public int ProviderId { get; set; }
    public Provider Provider { get; set; } = null!;

    public string Type { get; set; } = "stream"; // stream, rent, buy
    public string Country { get; set; } = "US";
}
