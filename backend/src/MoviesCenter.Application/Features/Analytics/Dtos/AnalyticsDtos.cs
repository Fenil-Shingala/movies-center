namespace MoviesCenter.Application.Features.Analytics.Dtos;

public sealed class AnalyticsEventDto
{
    public string EventName { get; set; } = string.Empty;
    public string? Dimension { get; set; }
    public string? SessionId { get; set; }
    public DateTimeOffset? Timestamp { get; set; }

    public AnalyticsEventDto() { }

    public AnalyticsEventDto(string eventName, string? dimension = null, string? sessionId = null, DateTimeOffset? timestamp = null)
    {
        EventName = eventName;
        Dimension = dimension;
        SessionId = sessionId;
        Timestamp = timestamp;
    }
}

public sealed class IngestEventsRequest
{
    public List<AnalyticsEventDto>? Events { get; set; } = new();

    public IngestEventsRequest() { }

    public IngestEventsRequest(List<AnalyticsEventDto>? events)
    {
        Events = events;
    }
}

public sealed record DailyMetricTrendDto(
    DateOnly Date,
    long Plays,
    long Searches,
    long Signups
);

public sealed record TopMetricItemDto(
    string Name,
    long Value,
    int? TitleId = null,
    bool? HasResults = null
);

public sealed record AnalyticsOverviewDto(
    long TotalPlays,
    long TotalSearches,
    long TotalEmptySearches,
    long TotalSignups,
    List<DailyMetricTrendDto> DailyTrends,
    List<TopMetricItemDto> TopPlayedTitles,
    List<TopMetricItemDto> TopSearchQueries,
    List<TopMetricItemDto> TopEmptySearches,
    List<TopMetricItemDto> FailingSources
);
