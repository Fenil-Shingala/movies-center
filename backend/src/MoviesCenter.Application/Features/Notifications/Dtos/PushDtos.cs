namespace MoviesCenter.Application.Features.Notifications.Dtos;

public record PushSubscriptionDto(
    string Endpoint,
    string P256dh,
    string Auth,
    string? UserAgent
);

public record VapidPublicKeyDto(
    string PublicKey
);

public record UnregisterPushSubscriptionRequest(
    string Endpoint
);
