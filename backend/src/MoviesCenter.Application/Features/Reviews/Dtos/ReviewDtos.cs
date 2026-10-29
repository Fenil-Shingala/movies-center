namespace MoviesCenter.Application.Features.Reviews.Dtos;

public sealed record ReviewDto(
    int Id,
    int TitleId,
    Guid UserId,
    string UserName,
    string? UserAvatar,
    string Content,
    int? RatingScore,
    bool ContainsSpoilers,
    int LikesCount,
    bool IsLikedByMe,
    bool IsMyReview,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public sealed record CreateReviewRequest(
    string Content,
    int? RatingScore = null,
    bool ContainsSpoilers = false
);

public sealed record ReviewsListResponseDto(
    List<ReviewDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public sealed record ToggleLikeResponseDto(
    bool IsLiked,
    int LikesCount
);
