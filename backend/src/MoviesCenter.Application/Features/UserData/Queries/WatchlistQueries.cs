using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.UserData.Dtos;

namespace MoviesCenter.Application.Features.UserData.Queries;

public sealed record GetWatchlistQuery(
    Guid UserId,
    string? Type = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<WatchlistItemDto>>;

public class GetWatchlistQueryHandler : IRequestHandler<GetWatchlistQuery, PagedResult<WatchlistItemDto>>
{
    private readonly IUserDataService _userDataService;

    public GetWatchlistQueryHandler(IUserDataService userDataService)
    {
        _userDataService = userDataService;
    }

    public async Task<PagedResult<WatchlistItemDto>> Handle(GetWatchlistQuery query, CancellationToken cancellationToken)
    {
        return await _userDataService.GetWatchlistAsync(query.UserId, query.Type, query.Page, query.PageSize, cancellationToken);
    }
}

public sealed record CheckWatchlistStatusQuery(Guid UserId, int TitleId) : IRequest<bool>;

public class CheckWatchlistStatusQueryHandler : IRequestHandler<CheckWatchlistStatusQuery, bool>
{
    private readonly IUserDataService _userDataService;

    public CheckWatchlistStatusQueryHandler(IUserDataService userDataService)
    {
        _userDataService = userDataService;
    }

    public async Task<bool> Handle(CheckWatchlistStatusQuery query, CancellationToken cancellationToken)
    {
        return await _userDataService.IsInWatchlistAsync(query.UserId, query.TitleId, cancellationToken);
    }
}
