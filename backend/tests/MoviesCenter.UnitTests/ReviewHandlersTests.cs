using FluentAssertions;
using Moq;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Reviews.Commands;
using MoviesCenter.Application.Features.Reviews.Dtos;
using MoviesCenter.Application.Features.Reviews.Queries;
using Xunit;

namespace MoviesCenter.UnitTests;

public class ReviewHandlersTests
{
    private readonly Mock<IReviewService> _mockReviewService = new();

    [Fact]
    public async Task GetTitleReviewsQueryHandler_ShouldReturnReviewsList()
    {
        // Arrange
        var titleId = 155;
        var expected = new ReviewsListResponseDto(
            Items:
            [
                new ReviewDto(
                    Id: 1,
                    TitleId: titleId,
                    UserId: Guid.NewGuid(),
                    UserName: "CinemaFan",
                    UserAvatar: null,
                    Content: "Masterpiece of cinema storytelling!",
                    RatingScore: 5,
                    ContainsSpoilers: false,
                    LikesCount: 12,
                    IsLikedByMe: false,
                    IsMyReview: false,
                    CreatedAt: DateTime.UtcNow,
                    UpdatedAt: DateTime.UtcNow
                )
            ],
            TotalCount: 1,
            Page: 1,
            PageSize: 10,
            TotalPages: 1
        );

        _mockReviewService
            .Setup(s => s.GetTitleReviewsAsync(titleId, 1, 10, "popular", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetTitleReviewsQueryHandler(_mockReviewService.Object);

        // Act
        var result = await handler.Handle(new GetTitleReviewsQuery(titleId), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expected);
        _mockReviewService.Verify(s => s.GetTitleReviewsAsync(titleId, 1, 10, "popular", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateReviewCommandHandler_ShouldDelegateToService()
    {
        // Arrange
        var titleId = 155;
        var userId = Guid.NewGuid();
        var request = new CreateReviewRequest("Outstanding cinematography!", 5, false);
        var expected = new ReviewDto(
            Id: 2,
            TitleId: titleId,
            UserId: userId,
            UserName: "Reviewer",
            UserAvatar: null,
            Content: "Outstanding cinematography!",
            RatingScore: 5,
            ContainsSpoilers: false,
            LikesCount: 0,
            IsLikedByMe: false,
            IsMyReview: true,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        _mockReviewService
            .Setup(s => s.CreateReviewAsync(titleId, userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new CreateReviewCommandHandler(_mockReviewService.Object);

        // Act
        var result = await handler.Handle(new CreateReviewCommand(titleId, userId, request), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expected);
        _mockReviewService.Verify(s => s.CreateReviewAsync(titleId, userId, request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteReviewCommandHandler_ShouldReturnTrueOnSuccess()
    {
        // Arrange
        var reviewId = 10;
        var userId = Guid.NewGuid();

        _mockReviewService
            .Setup(s => s.DeleteReviewAsync(reviewId, userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new DeleteReviewCommandHandler(_mockReviewService.Object);

        // Act
        var result = await handler.Handle(new DeleteReviewCommand(reviewId, userId, false), CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockReviewService.Verify(s => s.DeleteReviewAsync(reviewId, userId, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleReviewLikeCommandHandler_ShouldReturnUpdatedLikes()
    {
        // Arrange
        var reviewId = 5;
        var userId = Guid.NewGuid();
        var expected = new ToggleLikeResponseDto(IsLiked: true, LikesCount: 4);

        _mockReviewService
            .Setup(s => s.ToggleReviewLikeAsync(reviewId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new ToggleReviewLikeCommandHandler(_mockReviewService.Object);

        // Act
        var result = await handler.Handle(new ToggleReviewLikeCommand(reviewId, userId), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expected);
        _mockReviewService.Verify(s => s.ToggleReviewLikeAsync(reviewId, userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
