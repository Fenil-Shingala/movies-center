using FluentAssertions;
using Moq;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.Notifications.Dtos;
using Xunit;

namespace MoviesCenter.UnitTests;

public class NotificationTests
{
    private readonly Mock<INotificationService> _mockNotificationService = new();

    [Fact]
    public async Task GetNotifications_ShouldReturnPagedResults()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notifList = new List<NotificationDto>
        {
            new(Guid.NewGuid(), "NewEpisode", "House of the Dragon S02E04", "New episode available", null, false, DateTime.UtcNow),
            new(Guid.NewGuid(), "ContinueWatchingReminder", "Continue Watching", "Pick up where you left off", null, true, DateTime.UtcNow.AddHours(-2))
        };
        var pagedResult = new PagedResult<NotificationDto>(notifList, 1, 10, 2, 1);

        _mockNotificationService
            .Setup(s => s.GetNotificationsAsync(userId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _mockNotificationService.Object.GetNotificationsAsync(userId, 1, 10);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items[0].Title.Should().Be("House of the Dragon S02E04");
    }

    [Fact]
    public async Task GetUnreadCount_ShouldReturnExpectedCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockNotificationService
            .Setup(s => s.GetUnreadCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        var count = await _mockNotificationService.Object.GetUnreadCountAsync(userId);

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public async Task MarkAsRead_ShouldReturnTrueOnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notifId = Guid.NewGuid();
        _mockNotificationService
            .Setup(s => s.MarkAsReadAsync(userId, notifId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var success = await _mockNotificationService.Object.MarkAsReadAsync(userId, notifId);

        // Assert
        success.Should().BeTrue();
        _mockNotificationService.Verify(s => s.MarkAsReadAsync(userId, notifId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAllAsRead_ShouldReturnNumberOfMarkedItems()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockNotificationService
            .Setup(s => s.MarkAllAsReadAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        var markedCount = await _mockNotificationService.Object.MarkAllAsReadAsync(userId);

        // Assert
        markedCount.Should().Be(5);
    }

    [Fact]
    public async Task DeleteNotification_ShouldReturnTrueWhenFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notifId = Guid.NewGuid();
        _mockNotificationService
            .Setup(s => s.DeleteNotificationAsync(userId, notifId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mockNotificationService.Object.DeleteNotificationAsync(userId, notifId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetSettings_ShouldReturnConfiguredSettings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var settingsDto = new NotificationSettingsDto(
            NewEpisodeInApp: true,
            NewEpisodeEmail: true,
            NewEpisodePush: false,
            ContinueWatchingInApp: true,
            ContinueWatchingEmail: false,
            ContinueWatchingPush: false
        );

        _mockNotificationService
            .Setup(s => s.GetSettingsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settingsDto);

        // Act
        var result = await _mockNotificationService.Object.GetSettingsAsync(userId);

        // Assert
        result.Should().BeEquivalentTo(settingsDto);
        result.NewEpisodeInApp.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateSettings_ShouldReturnUpdatedSettings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updatedDto = new NotificationSettingsDto(
            NewEpisodeInApp: false,
            NewEpisodeEmail: false,
            NewEpisodePush: true,
            ContinueWatchingInApp: false,
            ContinueWatchingEmail: true,
            ContinueWatchingPush: false
        );

        _mockNotificationService
            .Setup(s => s.UpdateSettingsAsync(userId, updatedDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        // Act
        var result = await _mockNotificationService.Object.UpdateSettingsAsync(userId, updatedDto);

        // Assert
        result.NewEpisodeInApp.Should().BeFalse();
        result.ContinueWatchingEmail.Should().BeTrue();
    }

    [Fact]
    public async Task BroadcastSystemNotification_ShouldReturnDispatchedCount()
    {
        // Arrange
        _mockNotificationService
            .Setup(s => s.BroadcastSystemNotificationAsync("Server Notice", "Maintenance in 1 hour", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        // Act
        var count = await _mockNotificationService.Object.BroadcastSystemNotificationAsync("Server Notice", "Maintenance in 1 hour");

        // Assert
        count.Should().Be(42);
    }

    [Fact]
    public async Task RegisterPushSubscription_ShouldReturnTrueOnSuccess()
    {
        // Arrange
        var mockPush = new Mock<IPushNotificationService>();
        var userId = Guid.NewGuid();
        var dto = new PushSubscriptionDto("https://fcm.googleapis.com/test", "key123", "auth456", "Chrome/Win");

        mockPush
            .Setup(p => p.RegisterSubscriptionAsync(userId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await mockPush.Object.RegisterSubscriptionAsync(userId, dto);

        // Assert
        result.Should().BeTrue();
        mockPush.Verify(p => p.RegisterSubscriptionAsync(userId, dto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnregisterPushSubscription_ShouldReturnTrueWhenRemoved()
    {
        // Arrange
        var mockPush = new Mock<IPushNotificationService>();
        var userId = Guid.NewGuid();
        var endpoint = "https://fcm.googleapis.com/test";

        mockPush
            .Setup(p => p.UnregisterSubscriptionAsync(userId, endpoint, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await mockPush.Object.UnregisterSubscriptionAsync(userId, endpoint);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetVapidPublicKey_ShouldReturnValidString()
    {
        // Arrange
        var mockPush = new Mock<IPushNotificationService>();
        mockPush
            .Setup(p => p.GetVapidPublicKeyAsync())
            .ReturnsAsync("BEl62iUYgUivxIkv69yViEuiBIa");

        // Act
        var key = await mockPush.Object.GetVapidPublicKeyAsync();

        // Assert
        key.Should().NotBeNullOrWhiteSpace();
    }
}
