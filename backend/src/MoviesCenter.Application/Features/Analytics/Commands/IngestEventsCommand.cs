using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Analytics.Dtos;

namespace MoviesCenter.Application.Features.Analytics.Commands;

public sealed record IngestEventsCommand(List<AnalyticsEventDto> Events) : IRequest<bool>;

public class IngestEventsCommandHandler : IRequestHandler<IngestEventsCommand, bool>
{
    private readonly IAnalyticsService _analyticsService;

    public IngestEventsCommandHandler(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    public async Task<bool> Handle(IngestEventsCommand request, CancellationToken cancellationToken)
    {
        if (request.Events == null || request.Events.Count == 0)
        {
            return true;
        }

        // Bound batch size to max 50 events per request
        var safeBatch = request.Events.Take(50);
        await _analyticsService.IngestEventsAsync(safeBatch, cancellationToken);
        return true;
    }
}
