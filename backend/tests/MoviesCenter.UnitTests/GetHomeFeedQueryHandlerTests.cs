using FluentAssertions;
using Moq;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Home.Dtos;
using MoviesCenter.Application.Features.Home.Queries;
using Xunit;

namespace MoviesCenter.UnitTests;

public class GetHomeFeedQueryHandlerTests
{
    private readonly Mock<ICatalogQueryService> _mockCatalogService = new();

    [Fact]
    public async Task Handle_ShouldReturnFeedFromCatalogQueryService()
    {
        // Arrange
        var sampleHero = new HeroItemDto(
            TmdbId: 101,
            Type: "movie",
            Title: "Gladiator II",
            Overview: "Years after witnessing the death of the revered hero Maximus...",
            BackdropPath: "/backdrop.jpg",
            PosterPath: "/poster.jpg",
            Rating: 8.8,
            ReleaseDate: "2024-11-22",
            Year: "2024",
            RuntimeMinutes: 148,
            TotalSeasons: null,
            Genres: new List<string> { "Action", "Drama" }
        );

        var sampleRows = new List<ContentRowDto>
        {
            new("trending-movies", "Trending Movies", "movie", "/movies?sort=trending", new List<MediaCardDto>
            {
                new(101, "movie", "Gladiator II", "/poster.jpg", "/backdrop.jpg", 8.8, "2024", new List<string> { "Action" })
            })
        };

        var expectedFeed = new HomeFeedDto(sampleHero, sampleRows);

        _mockCatalogService
            .Setup(s => s.GetHomeFeedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedFeed);

        var handler = new GetHomeFeedQueryHandler(_mockCatalogService.Object);

        // Act
        var result = await handler.Handle(new GetHomeFeedQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedFeed);
        _mockCatalogService.Verify(s => s.GetHomeFeedAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
