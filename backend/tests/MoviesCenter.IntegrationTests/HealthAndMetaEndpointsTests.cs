using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using MoviesCenter.Domain.Constants;
using Xunit;

namespace MoviesCenter.IntegrationTests;

public class HealthAndMetaEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthAndMetaEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReturn200AndHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        root.GetProperty("status").GetString().Should().Be("Healthy");
        root.GetProperty("application").GetString().Should().Be(AppConstants.AppName);
    }

    [Fact]
    public async Task MetaInfoEndpoint_ShouldReturn200AndAppDetails()
    {
        // Act
        var response = await _client.GetAsync("/api/meta/info");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        root.GetProperty("appName").GetString().Should().Be(AppConstants.AppName);
        root.GetProperty("version").GetString().Should().Be(AppConstants.AppVersion);
    }
}
