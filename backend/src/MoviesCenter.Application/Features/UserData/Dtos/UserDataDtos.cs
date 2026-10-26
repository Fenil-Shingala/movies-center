namespace MoviesCenter.Application.Features.UserData.Dtos;

public sealed record WatchlistItemDto(
    int Id,
    int TitleId,
    string Title,
    string? PosterPath,
    string? BackdropPath,
    string Type,
    string? ReleaseDate,
    string? Year,
    double Rating,
    DateTime AddedAt
);

public sealed record AddToWatchlistRequest(int TitleId);

public sealed record WatchHistoryItemDto(
    int Id,
    int TitleId,
    string Title,
    string? PosterPath,
    string? BackdropPath,
    string Type,
    int? EpisodeId,
    int? SeasonNumber,
    int? EpisodeNumber,
    string? EpisodeName,
    int ProgressSeconds,
    int? DurationSeconds,
    int ProgressPercent,
    bool IsCompleted,
    DateTime WatchedAt
);

public sealed record UpdateWatchProgressRequest(
    int TitleId,
    int? EpisodeId = null,
    int? SeasonNumber = null,
    int? EpisodeNumber = null,
    int ProgressSeconds = 0,
    int? DurationSeconds = null
);
