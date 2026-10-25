using FluentAssertions;
using Moq;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Player.Commands;
using MoviesCenter.Application.Features.Player.Dtos;
using MoviesCenter.Application.Features.Player.Queries;
using Xunit;

namespace MoviesCenter.UnitTests;

public class PlayerSourcesQueryHandlerTests
{
    private readonly Mock<ICatalogQueryService> _mockCatalogQueryService = new();

    [Fact]
    public async Task GetTitleSources_ShouldReturnSourcesFromCatalogService()
    {
        // Arrange
        var expectedSources = new List<TitleSourceDto>
        {
            new(1, "VidSrc", "ThirdPartyEmbed", "https://vidsrc.to/embed/movie/155", "1080p", "Server 1 (VidSrc)", 1, false)
        };

        _mockCatalogQueryService
            .Setup(s => s.GetTitleSourcesAsync(155, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSources);

        var handler = new GetTitleSourcesQueryHandler(_mockCatalogQueryService.Object);

        // Act
        var result = await handler.Handle(new GetTitleSourcesQuery(155), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].ProviderName.Should().Be("VidSrc");
        result[0].Url.Should().Contain("vidsrc.to");
    }

    [Fact]
    public async Task ReportSource_ShouldReturnSuccessResponse()
    {
        // Arrange
        var command = new ReportSourceCommand(1, 155, "broken_stream", "Video not loading");
        var expectedResponse = new SourceReportResponseDto(true, "Report submitted");

        _mockCatalogQueryService
            .Setup(s => s.ReportSourceAsync(1, 155, "broken_stream", "Video not loading", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var handler = new ReportSourceCommandHandler(_mockCatalogQueryService.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }
}
