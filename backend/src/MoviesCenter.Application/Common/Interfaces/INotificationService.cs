using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.Notifications.Dtos;

namespace MoviesCenter.Application.Common.Interfaces;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
    Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteNotificationAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
    Task<NotificationSettingsDto> GetSettingsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<NotificationSettingsDto> UpdateSettingsAsync(Guid userId, NotificationSettingsDto dto, CancellationToken cancellationToken = default);
    Task<NotificationDto> CreateNotificationAsync(Guid userId, string type, string title, string body, string? data = null, CancellationToken cancellationToken = default);
    Task<int> BroadcastSystemNotificationAsync(string title, string body, string? data = null, string? type = null, CancellationToken cancellationToken = default);
    Task<int> SendContinueWatchingRemindersAsync(CancellationToken cancellationToken = default);
}
