using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MoviesCenter.IntegrationTests;

public class TitleDetailEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TitleDetailEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTitleDetail_WithExistingTitle_ShouldReturn200AndDetails()
    {
        // First get an active title tmdbId or id from browse
        var browseResp = await _client.GetAsync("/api/v1/titles?page=1&pageSize=1");
        browseResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var browseContent = await browseResp.Content.ReadAsStringAsync();
        using var browseDoc = JsonDocument.Parse(browseContent);
        var items = browseDoc.RootElement.GetProperty("items");

        if (items.GetArrayLength() > 0)
        {
            var firstItem = items[0];
            var tmdbId = firstItem.GetProperty("tmdbId").GetInt32();

            // Act
            var detailResp = await _client.GetAsync($"/api/v1/titles/{tmdbId}");

            // Assert
            detailResp.StatusCode.Should().Be(HttpStatusCode.OK);
            var detailContent = await detailResp.Content.ReadAsStringAsync();
            using var detailDoc = JsonDocument.Parse(detailContent);
            var root = detailDoc.RootElement;

            root.TryGetProperty("tmdbId", out var idProp).Should().BeTrue();
            idProp.GetInt32().Should().Be(tmdbId);
            root.TryGetProperty("title", out _).Should().BeTrue();
            root.TryGetProperty("type", out _).Should().BeTrue();
            root.TryGetProperty("genres", out var genresProp).Should().BeTrue();
            genresProp.ValueKind.Should().Be(JsonValueKind.Array);
        }
    }

    [Fact]
    public async Task GetTitleDetail_WithNonExistingId_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/titles/99999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTitleWhereToWatch_ShouldReturn200()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/titles/1/where-to-watch");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
    }
}
