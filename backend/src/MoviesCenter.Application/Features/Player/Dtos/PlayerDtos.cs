namespace MoviesCenter.Application.Features.Player.Dtos;

public record TitleSourceDto(
    int Id,
    string ProviderName,
    string SourceType,
    string Url,
    string Quality,
    string Label,
    int Priority,
    bool IsDirectStream
);

public record ReportSourceRequest(
    int TitleId,
    string Reason,
    string? Details = null,
    int? Season = null,
    int? Episode = null
);

public record SourceReportResponseDto(
    bool Success,
    string Message
);
