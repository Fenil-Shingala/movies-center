using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Auth.Commands;
using MoviesCenter.Application.Features.Auth.Dtos;
using MoviesCenter.Application.Features.Auth.Queries;
using MoviesCenter.Application.Features.Auth.Validators;
using Xunit;

namespace MoviesCenter.UnitTests;

public class AuthHandlersTests
{
    private readonly Mock<IAuthService> _mockAuthService = new();

    [Fact]
    public async Task RegisterCommandHandler_ShouldDelegateToAuthService()
    {
        // Arrange
        var request = new RegisterRequest("user@example.com", "Password123!", "MovieFan");
        var expectedResponse = new AuthResponseDto(
            "test_jwt_token",
            DateTime.UtcNow.AddMinutes(60),
            new UserProfileDto(Guid.NewGuid(), "user@example.com", "MovieFan", null, new List<string> { "User" }),
            "refresh_token_123"
        );

        _mockAuthService
            .Setup(s => s.RegisterAsync(request, "127.0.0.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var handler = new RegisterCommandHandler(_mockAuthService.Object);

        // Act
        var result = await handler.Handle(new RegisterCommand(request, "127.0.0.1"), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedResponse);
        _mockAuthService.Verify(s => s.RegisterAsync(request, "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginCommandHandler_ShouldDelegateToAuthService()
    {
        // Arrange
        var request = new LoginRequest("user@example.com", "Password123!");
        var expectedResponse = new AuthResponseDto(
            "test_jwt_token",
            DateTime.UtcNow.AddMinutes(60),
            new UserProfileDto(Guid.NewGuid(), "user@example.com", "MovieFan", null, new List<string> { "User" }),
            "refresh_token_123"
        );

        _mockAuthService
            .Setup(s => s.LoginAsync(request, "127.0.0.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var handler = new LoginCommandHandler(_mockAuthService.Object);

        // Act
        var result = await handler.Handle(new LoginCommand(request, "127.0.0.1"), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedResponse);
        _mockAuthService.Verify(s => s.LoginAsync(request, "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenCommandHandler_ShouldDelegateToAuthService()
    {
        // Arrange
        var token = "active_refresh_token";
        var expectedResponse = new AuthResponseDto(
            "new_jwt_token",
            DateTime.UtcNow.AddMinutes(60),
            new UserProfileDto(Guid.NewGuid(), "user@example.com", "MovieFan", null, new List<string> { "User" }),
            "new_refresh_token"
        );

        _mockAuthService
            .Setup(s => s.RefreshTokenAsync(token, "127.0.0.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var handler = new RefreshTokenCommandHandler(_mockAuthService.Object);

        // Act
        var result = await handler.Handle(new RefreshTokenCommand(token, "127.0.0.1"), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedResponse);
        _mockAuthService.Verify(s => s.RefreshTokenAsync(token, "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutCommandHandler_ShouldDelegateToAuthService()
    {
        // Arrange
        var token = "active_refresh_token";
        _mockAuthService
            .Setup(s => s.RevokeTokenAsync(token, "127.0.0.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new LogoutCommandHandler(_mockAuthService.Object);

        // Act
        var result = await handler.Handle(new LogoutCommand(token, "127.0.0.1"), CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockAuthService.Verify(s => s.RevokeTokenAsync(token, "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUserQueryHandler_ShouldReturnUserProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = new UserProfileDto(userId, "fan@movies.com", "CinemaLover", null, new List<string> { "User" });

        _mockAuthService
            .Setup(s => s.GetCurrentUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var handler = new GetCurrentUserQueryHandler(_mockAuthService.Object);

        // Act
        var result = await handler.Handle(new GetCurrentUserQuery(userId), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(profile);
    }

    [Fact]
    public async Task ImportGuestDataCommandHandler_ShouldReturnMergeResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new ImportGuestDataRequest(
            WatchlistTitleIds: new List<int> { 1, 2, 3 },
            History: new List<GuestHistoryItemDto>
            {
                new(TitleId: 1, ProgressSeconds: 300, DurationSeconds: 7200)
            }
        );
        var expectedResult = new GuestMergeResultDto(3, 1, "Guest data merged successfully.");

        _mockAuthService
            .Setup(s => s.ImportGuestDataAsync(userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var handler = new ImportGuestDataCommandHandler(_mockAuthService.Object);

        // Act
        var result = await handler.Handle(new ImportGuestDataCommand(userId, request), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public void RegisterRequestValidator_ShouldFail_WhenEmailIsInvalid()
    {
        var validator = new RegisterRequestValidator();
        var model = new RegisterRequest("invalid-email", "StrongPass1!", "Display");
        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void RegisterRequestValidator_ShouldFail_WhenPasswordTooShort()
    {
        var validator = new RegisterRequestValidator();
        var model = new RegisterRequest("user@example.com", "short", "Display");
        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void LoginRequestValidator_ShouldFail_WhenEmailOrPasswordEmpty()
    {
        var validator = new LoginRequestValidator();
        var model = new LoginRequest("", "");
        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
