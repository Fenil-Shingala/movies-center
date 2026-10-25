namespace MoviesCenter.Application.Features.Admin.Dtos;

public record TmdbSyncResultDto(
    bool Success,
    int ItemsSynced,
    int Errors,
    TimeSpan Duration,
    string? ErrorMessage = null
);

public record TmdbSyncStatusDto(
    bool IsRunning,
    DateTimeOffset? LastRunAt,
    string? LastStatus,
    int LastItemsSynced,
    int LastErrors,
    string? TriggeredBy = null,
    string? ErrorMessage = null
);
