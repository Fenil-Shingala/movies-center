namespace MoviesCenter.Application.Features.Auth.Dtos;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string DisplayName,
    GuestDataDto? GuestData = null
);

public sealed record LoginRequest(
    string Email,
    string Password,
    GuestDataDto? GuestData = null
);

public sealed record AuthResponseDto(
    string AccessToken,
    DateTime ExpiresAt,
    UserProfileDto User,
    string? RefreshToken = null
);

public sealed record UserProfileDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    List<string> Roles
);

public sealed record GuestDataDto(
    List<int>? WatchlistTitleIds = null,
    List<GuestHistoryItemDto>? History = null
);

public sealed record GuestHistoryItemDto(
    int TitleId,
    int? EpisodeId = null,
    int? SeasonNumber = null,
    int? EpisodeNumber = null,
    int ProgressSeconds = 0,
    int? DurationSeconds = null,
    DateTime? WatchedAt = null
);

public sealed record ImportGuestDataRequest(
    List<int>? WatchlistTitleIds = null,
    List<GuestHistoryItemDto>? History = null
);

public sealed record GuestMergeResultDto(
    int WatchlistCount,
    int HistoryCount,
    string Message
);
