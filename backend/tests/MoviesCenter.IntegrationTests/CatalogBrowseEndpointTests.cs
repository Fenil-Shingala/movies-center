using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MoviesCenter.IntegrationTests;

public class CatalogBrowseEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CatalogBrowseEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task BrowseMovies_ShouldReturn200AndPagedResult()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/titles?type=movie&page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        root.TryGetProperty("items", out var items).Should().BeTrue();
        root.TryGetProperty("page", out var page).Should().BeTrue();
        root.TryGetProperty("pageSize", out var pageSize).Should().BeTrue();
        root.TryGetProperty("totalCount", out _).Should().BeTrue();

        page.GetInt32().Should().Be(1);
        pageSize.GetInt32().Should().Be(10);
        items.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task BrowseSeries_WithSortAndPaging_ShouldReturn200()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/titles?type=series&sort=rating.desc&page=1&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        root.TryGetProperty("items", out var items).Should().BeTrue();
        items.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetGenres_ShouldReturn200AndNonEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/genres");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        root.ValueKind.Should().Be(JsonValueKind.Array);
        root.GetArrayLength().Should().BeGreaterThan(0);
    }
}
