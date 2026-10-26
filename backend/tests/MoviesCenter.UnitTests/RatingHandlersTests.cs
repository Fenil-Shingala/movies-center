using FluentAssertions;
using Moq;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Ratings.Commands;
using MoviesCenter.Application.Features.Ratings.Dtos;
using MoviesCenter.Application.Features.Ratings.Queries;
using Xunit;

namespace MoviesCenter.UnitTests;

public class RatingHandlersTests
{
    private readonly Mock<IRatingService> _mockRatingService = new();

    [Fact]
    public async Task GetTitleRatingQueryHandler_ShouldReturnRatingDto()
    {
        // Arrange
        var titleId = 12;
        var userId = Guid.NewGuid();
        var expected = new RatingDto(
            AverageRating: 4.5,
            TotalRatings: 2,
            UserRating: 5,
            Distribution: new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 1 }, { 5, 1 } }
        );

        _mockRatingService
            .Setup(s => s.GetTitleRatingAsync(titleId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetTitleRatingQueryHandler(_mockRatingService.Object);

        // Act
        var result = await handler.Handle(new GetTitleRatingQuery(titleId, userId), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expected);
        _mockRatingService.Verify(s => s.GetTitleRatingAsync(titleId, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitRatingCommandHandler_ShouldDelegateToService()
    {
        // Arrange
        var titleId = 12;
        var userId = Guid.NewGuid();
        var expected = new RatingDto(
            AverageRating: 4.0,
            TotalRatings: 1,
            UserRating: 4,
            Distribution: new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 1 }, { 5, 0 } }
        );

        _mockRatingService
            .Setup(s => s.SubmitRatingAsync(titleId, userId, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new SubmitRatingCommandHandler(_mockRatingService.Object);

        // Act
        var result = await handler.Handle(new SubmitRatingCommand(titleId, userId, 4), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expected);
        _mockRatingService.Verify(s => s.SubmitRatingAsync(titleId, userId, 4, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveRatingCommandHandler_ShouldDelegateToService()
    {
        // Arrange
        var titleId = 12;
        var userId = Guid.NewGuid();
        var expected = new RatingDto(
            AverageRating: 0.0,
            TotalRatings: 0,
            UserRating: null,
            Distribution: new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 } }
        );

        _mockRatingService
            .Setup(s => s.RemoveRatingAsync(titleId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new RemoveRatingCommandHandler(_mockRatingService.Object);

        // Act
        var result = await handler.Handle(new RemoveRatingCommand(titleId, userId), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expected);
        _mockRatingService.Verify(s => s.RemoveRatingAsync(titleId, userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
