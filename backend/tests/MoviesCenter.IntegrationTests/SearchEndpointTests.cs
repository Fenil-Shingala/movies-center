using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MoviesCenter.IntegrationTests;

public class SearchEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SearchEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Search_ShouldReturn200AndPagedResult()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/search?q=the&page=1&pageSize=10");

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
    public async Task SearchSuggest_ShouldReturn200AndArray()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/search/suggest?q=the&limit=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        root.ValueKind.Should().Be(JsonValueKind.Array);
        root.GetArrayLength().Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task SearchSuggest_WithShortQuery_ShouldReturnEmptyArray()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/search/suggest?q=a");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        root.ValueKind.Should().Be(JsonValueKind.Array);
        root.GetArrayLength().Should().Be(0);
    }
}
