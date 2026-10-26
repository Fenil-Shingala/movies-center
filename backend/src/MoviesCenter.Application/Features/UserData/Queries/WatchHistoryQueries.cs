using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.UserData.Dtos;

namespace MoviesCenter.Application.Features.UserData.Queries;

public sealed record GetWatchHistoryQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<WatchHistoryItemDto>>;

public class GetWatchHistoryQueryHandler : IRequestHandler<GetWatchHistoryQuery, PagedResult<WatchHistoryItemDto>>
{
    private readonly IUserDataService _userDataService;

    public GetWatchHistoryQueryHandler(IUserDataService userDataService)
    {
        _userDataService = userDataService;
    }

    public async Task<PagedResult<WatchHistoryItemDto>> Handle(GetWatchHistoryQuery query, CancellationToken cancellationToken)
    {
        return await _userDataService.GetWatchHistoryAsync(query.UserId, query.Page, query.PageSize, cancellationToken);
    }
}
