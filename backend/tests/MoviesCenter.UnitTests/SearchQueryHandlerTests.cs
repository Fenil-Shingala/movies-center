using FluentAssertions;
using Moq;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.Home.Dtos;
using MoviesCenter.Application.Features.Search.Dtos;
using MoviesCenter.Application.Features.Search.Queries;
using Xunit;

namespace MoviesCenter.UnitTests;

public class SearchQueryHandlerTests
{
    private readonly Mock<ICatalogQueryService> _mockCatalogQueryService = new();

    [Fact]
    public async Task SearchTitles_ShouldCallCatalogQueryServiceWithCorrectParameters()
    {
        // Arrange
        var query = new SearchTitlesQuery(
            Query: "batman",
            Type: "movie",
            Page: 1,
            PageSize: 12
        );

        var expectedResult = new PagedResult<MediaCardDto>(
            Items: new List<MediaCardDto>
            {
                new(155, "movie", "The Dark Knight", "/poster.jpg", "/backdrop.jpg", 9.0, "2008", new List<string> { "Action", "Crime" })
            },
            Page: 1,
            PageSize: 12,
            TotalCount: 1,
            TotalPages: 1
        );

        _mockCatalogQueryService
            .Setup(s => s.SearchTitlesAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var handler = new SearchTitlesQueryHandler(_mockCatalogQueryService.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedResult);
        result.Items.Should().HaveCount(1);
        _mockCatalogQueryService.Verify(s => s.SearchTitlesAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSearchSuggestions_ShouldReturnSuggestionsFromCatalogQueryService()
    {
        // Arrange
        var query = new GetSearchSuggestionsQuery("avengers", 5);
        var expectedSuggestions = new List<SearchSuggestionDto>
        {
            new(24428, "movie", "The Avengers", "/avengers.jpg", 8.0, "2012", new List<string> { "Action", "Sci-Fi" })
        };

        _mockCatalogQueryService
            .Setup(s => s.GetSearchSuggestionsAsync("avengers", 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSuggestions);

        var handler = new GetSearchSuggestionsQueryHandler(_mockCatalogQueryService.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("The Avengers");
        _mockCatalogQueryService.Verify(s => s.GetSearchSuggestionsAsync("avengers", 5, It.IsAny<CancellationToken>()), Times.Once);
    }
}
