using MoviesCenter.Application.Features.Notifications.Dtos;

namespace MoviesCenter.Application.Common.Interfaces;

public interface IPushNotificationService
{
    Task<bool> RegisterSubscriptionAsync(Guid userId, PushSubscriptionDto dto, CancellationToken cancellationToken = default);
    Task<bool> UnregisterSubscriptionAsync(Guid userId, string endpoint, CancellationToken cancellationToken = default);
    Task<string> GetVapidPublicKeyAsync();
    Task<int> SendPushNotificationAsync(Guid userId, string title, string body, string? url = null, CancellationToken cancellationToken = default);
}
