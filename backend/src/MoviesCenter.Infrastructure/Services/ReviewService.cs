using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Reviews.Dtos;
using MoviesCenter.Domain.Entities;
using MoviesCenter.Infrastructure.Data;

namespace MoviesCenter.Infrastructure.Services;

public class ReviewService : IReviewService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IRatingService _ratingService;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(
        ApplicationDbContext dbContext,
        IRatingService ratingService,
        ILogger<ReviewService> logger)
    {
        _dbContext = dbContext;
        _ratingService = ratingService;
        _logger = logger;
    }

    public async Task<ReviewsListResponseDto> GetTitleReviewsAsync(
        int titleId, 
        int page = 1, 
        int pageSize = 10, 
        string sort = "popular", 
        Guid? currentUserId = null, 
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        // Resolve title
        var title = await _dbContext.Titles
            .AsNoTracking()
            .FirstOrDefaultAsync(t => (t.Id == titleId || t.TmdbId == titleId) && t.IsActive, cancellationToken);

        if (title == null)
        {
            return new ReviewsListResponseDto([], 0, page, pageSize, 0);
        }

        var resolvedTitleId = title.Id;

        var query = _dbContext.Reviews
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.TitleId == resolvedTitleId);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        // Sorting
        query = (sort?.Trim().ToLowerInvariant()) switch
        {
            "newest" or "recent" => query.OrderByDescending(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.LikesCount).ThenByDescending(r => r.CreatedAt) // default popular
        };

        var reviews = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Check user likes
        var likedReviewIds = new HashSet<int>();
        if (currentUserId.HasValue && reviews.Count > 0)
        {
            var reviewIds = reviews.Select(r => r.Id).ToList();
            var userLikes = await _dbContext.ReviewLikes
                .AsNoTracking()
                .Where(rl => rl.UserId == currentUserId.Value && reviewIds.Contains(rl.ReviewId))
                .Select(rl => rl.ReviewId)
                .ToListAsync(cancellationToken);

            likedReviewIds = [..userLikes];
        }

        var items = reviews.Select(r => new ReviewDto(
            Id: r.Id,
            TitleId: title.TmdbId > 0 ? title.TmdbId : r.TitleId,
            UserId: r.UserId,
            UserName: !string.IsNullOrWhiteSpace(r.User.DisplayName) 
                ? r.User.DisplayName 
                : (!string.IsNullOrWhiteSpace(r.User.UserName) ? r.User.UserName : "Movie Fan"),
            UserAvatar: r.User.AvatarUrl,
            Content: r.Content,
            RatingScore: r.RatingScore,
            ContainsSpoilers: r.ContainsSpoilers,
            LikesCount: r.LikesCount,
            IsLikedByMe: likedReviewIds.Contains(r.Id),
            IsMyReview: currentUserId.HasValue && r.UserId == currentUserId.Value,
            CreatedAt: r.CreatedAt,
            UpdatedAt: r.UpdatedAt
        )).ToList();

        return new ReviewsListResponseDto(items, totalCount, page, pageSize, totalPages);
    }

    public async Task<ReviewDto> CreateReviewAsync(
        int titleId, 
        Guid userId, 
        CreateReviewRequest request, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ArgumentException("Review content cannot be empty.", nameof(request.Content));
        }

        var cleanContent = request.Content.Trim();
        if (cleanContent.Length > 2000)
        {
            cleanContent = cleanContent[..2000];
        }

        // Resolve title
        var title = await _dbContext.Titles
            .FirstOrDefaultAsync(t => (t.Id == titleId || t.TmdbId == titleId) && t.IsActive, cancellationToken);

        if (title == null)
        {
            throw new KeyNotFoundException($"Title with ID {titleId} not found.");
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }

        int? validatedScore = null;
        if (request.RatingScore.HasValue && request.RatingScore.Value is >= 1 and <= 5)
        {
            validatedScore = request.RatingScore.Value;
            // Also sync user's star rating
            try
            {
                await _ratingService.SubmitRatingAsync(title.Id, userId, validatedScore.Value, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sync rating score for user {UserId} on title {TitleId}", userId, title.Id);
            }
        }

        var review = new Review
        {
            UserId = userId,
            TitleId = title.Id,
            Content = cleanContent,
            RatingScore = validatedScore,
            ContainsSpoilers = request.ContainsSpoilers,
            LikesCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Reviews.Add(review);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new review {ReviewId} by user {UserId} for title {TitleId}", review.Id, userId, title.Id);

        return new ReviewDto(
            Id: review.Id,
            TitleId: title.TmdbId > 0 ? title.TmdbId : title.Id,
            UserId: user.Id,
            UserName: !string.IsNullOrWhiteSpace(user.DisplayName) ? user.DisplayName : (user.UserName ?? "Movie Fan"),
            UserAvatar: user.AvatarUrl,
            Content: review.Content,
            RatingScore: review.RatingScore,
            ContainsSpoilers: review.ContainsSpoilers,
            LikesCount: review.LikesCount,
            IsLikedByMe: false,
            IsMyReview: true,
            CreatedAt: review.CreatedAt,
            UpdatedAt: review.UpdatedAt
        );
    }

    public async Task<bool> DeleteReviewAsync(
        int reviewId, 
        Guid userId, 
        bool isAdmin, 
        CancellationToken cancellationToken = default)
    {
        var review = await _dbContext.Reviews
            .FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);

        if (review == null)
        {
            return false;
        }

        // Only author or admin can delete
        if (review.UserId != userId && !isAdmin)
        {
            _logger.LogWarning("Unauthorized attempt to delete review {ReviewId} by user {UserId}", reviewId, userId);
            return false;
        }

        _dbContext.Reviews.Remove(review);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted review {ReviewId} by user {UserId} (IsAdmin: {IsAdmin})", reviewId, userId, isAdmin);
        return true;
    }

    public async Task<ToggleLikeResponseDto> ToggleReviewLikeAsync(
        int reviewId, 
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var review = await _dbContext.Reviews
            .FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);

        if (review == null)
        {
            throw new KeyNotFoundException($"Review with ID {reviewId} not found.");
        }

        var existingLike = await _dbContext.ReviewLikes
            .FirstOrDefaultAsync(rl => rl.ReviewId == reviewId && rl.UserId == userId, cancellationToken);

        bool isLiked;
        if (existingLike != null)
        {
            _dbContext.ReviewLikes.Remove(existingLike);
            review.LikesCount = Math.Max(0, review.LikesCount - 1);
            isLiked = false;
        }
        else
        {
            _dbContext.ReviewLikes.Add(new ReviewLike
            {
                ReviewId = reviewId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            review.LikesCount++;
            isLiked = true;
        }

        review.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ToggleLikeResponseDto(isLiked, review.LikesCount);
    }
}
