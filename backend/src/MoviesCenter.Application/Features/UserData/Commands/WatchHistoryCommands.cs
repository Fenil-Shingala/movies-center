using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.UserData.Dtos;

namespace MoviesCenter.Application.Features.UserData.Commands;

public sealed record UpdateWatchProgressCommand(Guid UserId, UpdateWatchProgressRequest Request) : IRequest<WatchHistoryItemDto>;

public class UpdateWatchProgressCommandHandler : IRequestHandler<UpdateWatchProgressCommand, WatchHistoryItemDto>
{
    private readonly IUserDataService _userDataService;

    public UpdateWatchProgressCommandHandler(IUserDataService userDataService)
    {
        _userDataService = userDataService;
    }

    public async Task<WatchHistoryItemDto> Handle(UpdateWatchProgressCommand command, CancellationToken cancellationToken)
    {
        return await _userDataService.UpdateWatchProgressAsync(command.UserId, command.Request, cancellationToken);
    }
}

public sealed record RemoveFromWatchHistoryCommand(Guid UserId, int HistoryId) : IRequest<bool>;

public class RemoveFromWatchHistoryCommandHandler : IRequestHandler<RemoveFromWatchHistoryCommand, bool>
{
    private readonly IUserDataService _userDataService;

    public RemoveFromWatchHistoryCommandHandler(IUserDataService userDataService)
    {
        _userDataService = userDataService;
    }

    public async Task<bool> Handle(RemoveFromWatchHistoryCommand command, CancellationToken cancellationToken)
    {
        return await _userDataService.RemoveFromWatchHistoryAsync(command.UserId, command.HistoryId, cancellationToken);
    }
}

public sealed record ClearWatchHistoryCommand(Guid UserId) : IRequest<bool>;

public class ClearWatchHistoryCommandHandler : IRequestHandler<ClearWatchHistoryCommand, bool>
{
    private readonly IUserDataService _userDataService;

    public ClearWatchHistoryCommandHandler(IUserDataService userDataService)
    {
        _userDataService = userDataService;
    }

    public async Task<bool> Handle(ClearWatchHistoryCommand command, CancellationToken cancellationToken)
    {
        return await _userDataService.ClearWatchHistoryAsync(command.UserId, cancellationToken);
    }
}
