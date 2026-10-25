using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using MoviesCenter.Application.Features.Player.Dtos;
using Xunit;

namespace MoviesCenter.IntegrationTests;

public class PlayerSourcesEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PlayerSourcesEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTitleSources_ForActiveTitle_ShouldReturn200AndNonEmptyList()
    {
        // Act (969681 is an active synced title)
        var response = await _client.GetAsync("/api/v1/titles/969681/sources");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        root.ValueKind.Should().Be(JsonValueKind.Array);
        root.GetArrayLength().Should().BeGreaterThan(0);

        var first = root[0];
        first.TryGetProperty("url", out var url).Should().BeTrue();
        url.GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ReportSource_ShouldReturn200AndSuccess()
    {
        // Arrange
        var request = new ReportSourceRequest(
            TitleId: 969681,
            Reason: "broken_stream",
            Details: "The embed fails to load"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/sources/1/report", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        root.TryGetProperty("success", out var success).Should().BeTrue();
        success.GetBoolean().Should().BeTrue();
    }
}
