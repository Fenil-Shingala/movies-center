using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Auth.Dtos;

namespace MoviesCenter.Application.Features.Auth.Commands;

public sealed record RegisterCommand(RegisterRequest Request, string? IpAddress = null) : IRequest<AuthResponseDto>;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly IAuthService _authService;

    public RegisterCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        return await _authService.RegisterAsync(command.Request, command.IpAddress, cancellationToken);
    }
}

public sealed record LoginCommand(LoginRequest Request, string? IpAddress = null) : IRequest<AuthResponseDto>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IAuthService _authService;

    public LoginCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        return await _authService.LoginAsync(command.Request, command.IpAddress, cancellationToken);
    }
}

public sealed record RefreshTokenCommand(string RefreshToken, string? IpAddress = null) : IRequest<AuthResponseDto>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IAuthService _authService;

    public RefreshTokenCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        return await _authService.RefreshTokenAsync(command.RefreshToken, command.IpAddress, cancellationToken);
    }
}

public sealed record LogoutCommand(string RefreshToken, string? IpAddress = null) : IRequest<bool>;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, bool>
{
    private readonly IAuthService _authService;

    public LogoutCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<bool> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        return await _authService.RevokeTokenAsync(command.RefreshToken, command.IpAddress, cancellationToken);
    }
}

public sealed record ImportGuestDataCommand(Guid UserId, ImportGuestDataRequest Request) : IRequest<GuestMergeResultDto>;

public class ImportGuestDataCommandHandler : IRequestHandler<ImportGuestDataCommand, GuestMergeResultDto>
{
    private readonly IAuthService _authService;

    public ImportGuestDataCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<GuestMergeResultDto> Handle(ImportGuestDataCommand command, CancellationToken cancellationToken)
    {
        return await _authService.ImportGuestDataAsync(command.UserId, command.Request, cancellationToken);
    }
}
