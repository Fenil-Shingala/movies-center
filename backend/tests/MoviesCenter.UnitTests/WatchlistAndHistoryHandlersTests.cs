using FluentAssertions;
using Moq;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.UserData.Commands;
using MoviesCenter.Application.Features.UserData.Dtos;
using MoviesCenter.Application.Features.UserData.Queries;
using Xunit;

namespace MoviesCenter.UnitTests;

public class WatchlistAndHistoryHandlersTests
{
    private readonly Mock<IUserDataService> _mockUserDataService = new();

    [Fact]
    public async Task GetWatchlistQueryHandler_ShouldReturnPagedWatchlist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var paged = new PagedResult<WatchlistItemDto>(
            Items: new List<WatchlistItemDto>
            {
                new(1, 10, "Inception", "/inception.jpg", "/bg.jpg", "movie", "2010-07-16", "2010", 8.8, DateTime.UtcNow)
            },
            Page: 1,
            PageSize: 20,
            TotalCount: 1,
            TotalPages: 1
        );

        _mockUserDataService
            .Setup(s => s.GetWatchlistAsync(userId, "movie", 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var handler = new GetWatchlistQueryHandler(_mockUserDataService.Object);

        // Act
        var result = await handler.Handle(new GetWatchlistQuery(userId, "movie", 1, 20), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(paged);
    }

    [Fact]
    public async Task AddToWatchlistCommandHandler_ShouldDelegateToService()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserDataService
            .Setup(s => s.AddToWatchlistAsync(userId, 55, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new AddToWatchlistCommandHandler(_mockUserDataService.Object);

        // Act
        var result = await handler.Handle(new AddToWatchlistCommand(userId, 55), CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockUserDataService.Verify(s => s.AddToWatchlistAsync(userId, 55, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveFromWatchlistCommandHandler_ShouldDelegateToService()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserDataService
            .Setup(s => s.RemoveFromWatchlistAsync(userId, 55, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new RemoveFromWatchlistCommandHandler(_mockUserDataService.Object);

        // Act
        var result = await handler.Handle(new RemoveFromWatchlistCommand(userId, 55), CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckWatchlistStatusQueryHandler_ShouldReturnStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserDataService
            .Setup(s => s.IsInWatchlistAsync(userId, 55, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CheckWatchlistStatusQueryHandler(_mockUserDataService.Object);

        // Act
        var result = await handler.Handle(new CheckWatchlistStatusQuery(userId, 55), CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateWatchProgressCommandHandler_ShouldReturnUpdatedItem()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateWatchProgressRequest(10, 101, 1, 1, 1200, 3600);
        var expectedItem = new WatchHistoryItemDto(
            1, 10, "Breaking Bad", "/poster.jpg", "/bg.jpg", "series",
            101, 1, 1, "Pilot", 1200, 3600, 33, false, DateTime.UtcNow
        );

        _mockUserDataService
            .Setup(s => s.UpdateWatchProgressAsync(userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItem);

        var handler = new UpdateWatchProgressCommandHandler(_mockUserDataService.Object);

        // Act
        var result = await handler.Handle(new UpdateWatchProgressCommand(userId, request), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedItem);
    }

    [Fact]
    public async Task ClearWatchHistoryCommandHandler_ShouldDelegateToService()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserDataService
            .Setup(s => s.ClearWatchHistoryAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new ClearWatchHistoryCommandHandler(_mockUserDataService.Object);

        // Act
        var result = await handler.Handle(new ClearWatchHistoryCommand(userId), CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockUserDataService.Verify(s => s.ClearWatchHistoryAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
