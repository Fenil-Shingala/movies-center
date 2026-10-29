using MoviesCenter.Domain.Common;
using MoviesCenter.Domain.Entities.Identity;

namespace MoviesCenter.Domain.Entities;

public class Review : BaseEntity
{
    public Guid UserId { get; set; }
    public int TitleId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? RatingScore { get; set; } // 1..5 stars
    public bool ContainsSpoilers { get; set; } = false;
    public int LikesCount { get; set; } = 0;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Title Title { get; set; } = null!;
    public ICollection<ReviewLike> ReviewLikes { get; set; } = new List<ReviewLike>();
}
