using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Player.Dtos;

namespace MoviesCenter.Application.Features.Player.Commands;

public record ReportSourceCommand(
    int SourceId,
    int TitleId,
    string Reason,
    string? Details = null,
    int? Season = null,
    int? Episode = null
) : IRequest<SourceReportResponseDto>;

public class ReportSourceCommandHandler : IRequestHandler<ReportSourceCommand, SourceReportResponseDto>
{
    private readonly ICatalogQueryService _catalogService;

    public ReportSourceCommandHandler(ICatalogQueryService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<SourceReportResponseDto> Handle(ReportSourceCommand request, CancellationToken cancellationToken)
    {
        return await _catalogService.ReportSourceAsync(
            request.SourceId,
            request.TitleId,
            request.Reason,
            request.Details,
            request.Season,
            request.Episode,
            cancellationToken
        );
    }
}
