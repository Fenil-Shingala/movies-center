using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MoviesCenter.IntegrationTests;

public class HomeEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HomeEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHomeFeed_ShouldReturn200AndValidStructure()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/home");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        root.TryGetProperty("hero", out var hero).Should().BeTrue();
        root.TryGetProperty("rows", out var rows).Should().BeTrue();

        hero.TryGetProperty("title", out _).Should().BeTrue();
        rows.ValueKind.Should().Be(JsonValueKind.Array);
    }
}
