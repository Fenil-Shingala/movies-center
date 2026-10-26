using MediatR;
using MoviesCenter.Application.Common.Interfaces;

namespace MoviesCenter.Application.Features.UserData.Commands;

public sealed record AddToWatchlistCommand(Guid UserId, int TitleId) : IRequest<bool>;

public class AddToWatchlistCommandHandler : IRequestHandler<AddToWatchlistCommand, bool>
{
    private readonly IUserDataService _userDataService;

    public AddToWatchlistCommandHandler(IUserDataService userDataService)
    {
        _userDataService = userDataService;
    }

    public async Task<bool> Handle(AddToWatchlistCommand command, CancellationToken cancellationToken)
    {
        return await _userDataService.AddToWatchlistAsync(command.UserId, command.TitleId, cancellationToken);
    }
}

public sealed record RemoveFromWatchlistCommand(Guid UserId, int TitleId) : IRequest<bool>;

public class RemoveFromWatchlistCommandHandler : IRequestHandler<RemoveFromWatchlistCommand, bool>
{
    private readonly IUserDataService _userDataService;

    public RemoveFromWatchlistCommandHandler(IUserDataService userDataService)
    {
        _userDataService = userDataService;
    }

    public async Task<bool> Handle(RemoveFromWatchlistCommand command, CancellationToken cancellationToken)
    {
        return await _userDataService.RemoveFromWatchlistAsync(command.UserId, command.TitleId, cancellationToken);
    }
}
