using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Analytics.Dtos;

namespace MoviesCenter.Application.Features.Analytics.Queries;

public sealed record GetAnalyticsOverviewQuery(DateOnly? StartDate = null, DateOnly? EndDate = null) : IRequest<AnalyticsOverviewDto>;

public class GetAnalyticsOverviewQueryHandler : IRequestHandler<GetAnalyticsOverviewQuery, AnalyticsOverviewDto>
{
    private readonly IAnalyticsService _analyticsService;

    public GetAnalyticsOverviewQueryHandler(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    public async Task<AnalyticsOverviewDto> Handle(GetAnalyticsOverviewQuery request, CancellationToken cancellationToken)
    {
        return await _analyticsService.GetAnalyticsOverviewAsync(request.StartDate, request.EndDate, cancellationToken);
    }
}
