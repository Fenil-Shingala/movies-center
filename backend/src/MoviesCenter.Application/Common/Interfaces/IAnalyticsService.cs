using MoviesCenter.Application.Features.Analytics.Dtos;

namespace MoviesCenter.Application.Common.Interfaces;

public interface IAnalyticsService
{
    Task IngestEventsAsync(IEnumerable<AnalyticsEventDto> events, CancellationToken cancellationToken = default);
    Task<AnalyticsOverviewDto> GetAnalyticsOverviewAsync(DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken cancellationToken = default);
}
