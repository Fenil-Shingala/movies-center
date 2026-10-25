using FluentAssertions;
using Moq;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Catalog.Dtos;
using MoviesCenter.Application.Features.Catalog.Queries;
using Xunit;

namespace MoviesCenter.UnitTests;

public class TitleDetailQueryHandlerTests
{
    private readonly Mock<ICatalogQueryService> _mockCatalogQueryService = new();

    [Fact]
    public async Task GetTitleDetail_ShouldReturnTitleDetailsFromCatalogQueryService()
    {
        // Arrange
        var expectedTitle = new TitleDetailDto(
            Id: 1,
            TmdbId: 155,
            Type: "movie",
            Title: "The Dark Knight",
            OriginalTitle: "The Dark Knight",
            Tagline: "Why So Serious?",
            Overview: "Batman raises the stakes in his war on crime.",
            PosterPath: "/poster.jpg",
            BackdropPath: "/backdrop.jpg",
            Rating: 9.0,
            VoteCount: 30000,
            ReleaseDate: "2008-07-18",
            Year: "2008",
            RuntimeMinutes: 152,
            Status: "Released",
            ContentRating: "PG-13",
            OriginalLanguage: "en",
            TotalSeasons: null,
            TotalEpisodes: null,
            Genres: new List<string> { "Action", "Crime", "Drama" },
            Cast: new List<CastMemberDto>
            {
                new(1, 3894, "Christian Bale", "Bruce Wayne / Batman", "/bale.jpg", 0)
            },
            Videos: new List<VideoDto>
            {
                new(1, "EXeTwQWrcwY", "Official Trailer", "Trailer", "YouTube")
            },
            Providers: new List<WatchProviderDto>
            {
                new(1, 8, "Netflix", "/netflix.jpg", "flatrate", "US")
            },
            Seasons: new List<SeasonDto>()
        );

        _mockCatalogQueryService
            .Setup(s => s.GetTitleDetailAsync(155, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTitle);

        var handler = new GetTitleDetailQueryHandler(_mockCatalogQueryService.Object);

        // Act
        var result = await handler.Handle(new GetTitleDetailQuery(155), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("The Dark Knight");
        result.TmdbId.Should().Be(155);
        result.Cast.Should().HaveCount(1);
        result.Videos.Should().HaveCount(1);
        result.Providers.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetTitleSeasons_ShouldReturnSeasonsList()
    {
        // Arrange
        var expectedSeasons = new List<SeasonDto>
        {
            new(1, 100, 1, "Season 1", "The first season", "/s1.jpg", "2022-01-01", 8, new List<EpisodeDto>())
        };

        _mockCatalogQueryService
            .Setup(s => s.GetTitleSeasonsAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSeasons);

        var handler = new GetTitleSeasonsQueryHandler(_mockCatalogQueryService.Object);

        // Act
        var result = await handler.Handle(new GetTitleSeasonsQuery(50), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].SeasonNumber.Should().Be(1);
    }

    [Fact]
    public async Task GetTitleWhereToWatch_ShouldReturnProvidersList()
    {
        // Arrange
        var expectedProviders = new List<WatchProviderDto>
        {
            new(1, 8, "Netflix", "/netflix.jpg", "flatrate", "US")
        };

        _mockCatalogQueryService
            .Setup(s => s.GetTitleWhereToWatchAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProviders);

        var handler = new GetTitleWhereToWatchQueryHandler(_mockCatalogQueryService.Object);

        // Act
        var result = await handler.Handle(new GetTitleWhereToWatchQuery(50), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Netflix");
    }
}
