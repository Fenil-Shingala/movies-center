namespace MoviesCenter.Application.Features.Notifications.Dtos;

public record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Body,
    string? Data,
    bool IsRead,
    DateTime CreatedAt
);

public record UnreadCountDto(
    int UnreadCount
);

public record NotificationSettingsDto(
    bool NewEpisodeInApp,
    bool NewEpisodeEmail,
    bool NewEpisodePush,
    bool ContinueWatchingInApp,
    bool ContinueWatchingEmail,
    bool ContinueWatchingPush
);

public record CreateNotificationRequest(
    string Type,
    string Title,
    string Body,
    string? Data
);

public record BroadcastNotificationRequest(
    string Title,
    string Body,
    string? Data,
    string? Type
);
