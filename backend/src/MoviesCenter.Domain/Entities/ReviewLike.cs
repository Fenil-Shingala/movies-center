using MoviesCenter.Domain.Common;
using MoviesCenter.Domain.Entities.Identity;

namespace MoviesCenter.Domain.Entities;

public class ReviewLike : BaseEntity
{
    public Guid UserId { get; set; }
    public int ReviewId { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Review Review { get; set; } = null!;
}
