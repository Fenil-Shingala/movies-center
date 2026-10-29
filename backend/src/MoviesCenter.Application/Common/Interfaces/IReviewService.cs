using MoviesCenter.Application.Features.Reviews.Dtos;

namespace MoviesCenter.Application.Common.Interfaces;

public interface IReviewService
{
    Task<ReviewsListResponseDto> GetTitleReviewsAsync(int titleId, int page = 1, int pageSize = 10, string sort = "popular", Guid? currentUserId = null, CancellationToken cancellationToken = default);
    Task<ReviewDto> CreateReviewAsync(int titleId, Guid userId, CreateReviewRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteReviewAsync(int reviewId, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<ToggleLikeResponseDto> ToggleReviewLikeAsync(int reviewId, Guid userId, CancellationToken cancellationToken = default);
}
