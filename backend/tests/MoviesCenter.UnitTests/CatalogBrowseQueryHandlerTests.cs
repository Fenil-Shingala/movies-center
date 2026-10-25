using FluentAssertions;
using Moq;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.Catalog.Dtos;
using MoviesCenter.Application.Features.Catalog.Queries;
using MoviesCenter.Application.Features.Home.Dtos;
using Xunit;

namespace MoviesCenter.UnitTests;

public class CatalogBrowseQueryHandlerTests
{
    private readonly Mock<ICatalogQueryService> _mockCatalogQueryService = new();

    [Fact]
    public async Task BrowseTitles_ShouldCallCatalogQueryServiceWithCorrectParameters()
    {
        // Arrange
        var query = new BrowseTitlesQuery(
            Type: "movie",
            GenreId: 28,
            Year: 2024,
            Language: "en",
            Sort: "rating.desc",
            Page: 2,
            PageSize: 10
        );

        var expectedResult = new PagedResult<MediaCardDto>(
            Items: new List<MediaCardDto>
            {
                new(1, "movie", "Sample Movie", "/poster.jpg", "/backdrop.jpg", 8.5, "2024", new List<string> { "Action" })
            },
            Page: 2,
            PageSize: 10,
            TotalCount: 35,
            TotalPages: 4
        );

        _mockCatalogQueryService
            .Setup(s => s.BrowseTitlesAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var handler = new BrowseTitlesQueryHandler(_mockCatalogQueryService.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedResult);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
        _mockCatalogQueryService.Verify(s => s.BrowseTitlesAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetGenres_ShouldReturnGenresListFromCatalogQueryService()
    {
        // Arrange
        var expectedGenres = new List<GenreDto>
        {
            new(28, "Action", "action"),
            new(35, "Comedy", "comedy"),
            new(18, "Drama", "drama")
        };

        _mockCatalogQueryService
            .Setup(s => s.GetGenresAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedGenres);

        var handler = new GetGenresQueryHandler(_mockCatalogQueryService.Object);

        // Act
        var result = await handler.Handle(new GetGenresQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(expectedGenres);
        _mockCatalogQueryService.Verify(s => s.GetGenresAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
