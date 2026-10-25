using FluentAssertions;
using Moq;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Home.Dtos;
using MoviesCenter.Application.Features.Home.Queries;
using Xunit;

namespace MoviesCenter.UnitTests;

public class GetHomeFeedQueryHandlerTests
{
    private readonly Mock<ITmdbService> _mockTmdbService = new();
    private readonly Mock<ICacheService> _mockCacheService = new();

    [Fact]
    public async Task Handle_WhenCached_ShouldReturnCachedFeedDirectly()
    {
        // Arrange
        var cachedHero = new HeroItemDto(
            TmdbId: 101,
            Type: "movie",
            Title: "Cached Movie",
            Overview: "Cached overview",
            BackdropPath: "/cached.jpg",
            PosterPath: "/cached_p.jpg",
            Rating: 8.8,
            ReleaseDate: "2024-01-01",
            Year: "2024",
            RuntimeMinutes: 120,
            TotalSeasons: null,
            Genres: new List<string> { "Sci-Fi" }
        );
        var cachedFeed = new HomeFeedDto(cachedHero, new List<ContentRowDto>());

        _mockCacheService
            .Setup(c => c.GetAsync<HomeFeedDto>("home_feed_v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedFeed);

        var handler = new GetHomeFeedQueryHandler(_mockTmdbService.Object, _mockCacheService.Object);

        // Act
        var result = await handler.Handle(new GetHomeFeedQuery(), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(cachedFeed);
        _mockTmdbService.Verify(s => s.GetTrendingMoviesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNotCached_ShouldFetchFromTmdbAndCache()
    {
        // Arrange
        _mockCacheService
            .Setup(c => c.GetAsync<HomeFeedDto>("home_feed_v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((HomeFeedDto?)null);

        var sampleMovies = new List<MediaCardDto>
        {
            new(201, "movie", "Trending One", "/p1.jpg", "/b1.jpg", 8.4, "2024", new List<string> { "Action" }),
            new(202, "movie", "Trending Two", "/p2.jpg", "/b2.jpg", 7.9, "2024", new List<string> { "Drama" })
        };

        var sampleTv = new List<MediaCardDto>
        {
            new(301, "series", "TV One", "/tv1.jpg", "/tvb1.jpg", 8.9, "2024", new List<string> { "Sci-Fi" })
        };

        _mockTmdbService.Setup(s => s.GetTrendingMoviesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sampleMovies);
        _mockTmdbService.Setup(s => s.GetTrendingTvAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sampleTv);
        _mockTmdbService.Setup(s => s.GetPopularMoviesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sampleMovies);
        _mockTmdbService.Setup(s => s.GetTopRatedMoviesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sampleMovies);
        _mockTmdbService.Setup(s => s.GetPopularTvAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sampleTv);
        _mockTmdbService.Setup(s => s.GetUpcomingMoviesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sampleMovies);

        var heroDetails = new HeroItemDto(
            TmdbId: 201,
            Type: "movie",
            Title: "Trending One",
            Overview: "Detailed overview of trending movie",
            BackdropPath: "/b1.jpg",
            PosterPath: "/p1.jpg",
            Rating: 8.4,
            ReleaseDate: "2024-05-01",
            Year: "2024",
            RuntimeMinutes: 145,
            TotalSeasons: null,
            Genres: new List<string> { "Action", "Adventure" }
        );

        _mockTmdbService.Setup(s => s.GetHeroDetailsAsync(201, "movie", It.IsAny<CancellationToken>()))
            .ReturnsAsync(heroDetails);

        var handler = new GetHomeFeedQueryHandler(_mockTmdbService.Object, _mockCacheService.Object);

        // Act
        var result = await handler.Handle(new GetHomeFeedQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Hero.TmdbId.Should().Be(201);
        result.Hero.Title.Should().Be("Trending One");
        result.Rows.Should().HaveCount(6);
        result.Rows.First().Id.Should().Be("trending-movies");
        result.Rows.First().Items.Should().HaveCount(2);

        _mockCacheService.Verify(c => c.SetAsync("home_feed_v1", It.IsAny<HomeFeedDto>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
