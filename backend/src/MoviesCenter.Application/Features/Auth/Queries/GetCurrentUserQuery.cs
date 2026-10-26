using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Auth.Dtos;

namespace MoviesCenter.Application.Features.Auth.Queries;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<UserProfileDto?>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserProfileDto?>
{
    private readonly IAuthService _authService;

    public GetCurrentUserQueryHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<UserProfileDto?> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        return await _authService.GetCurrentUserAsync(query.UserId, cancellationToken);
    }
}
