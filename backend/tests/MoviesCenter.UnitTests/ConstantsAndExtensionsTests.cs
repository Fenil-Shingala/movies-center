using FluentAssertions;
using MoviesCenter.Domain.Constants;
using MoviesCenter.Domain.Entities;
using MoviesCenter.Infrastructure.Extensions;
using Xunit;

namespace MoviesCenter.UnitTests;

public class ConstantsAndExtensionsTests
{
    [Fact]
    public void AppConstants_AppName_ShouldBeMoviesCenter()
    {
        AppConstants.AppName.Should().Be("Movies Center");
        AppConstants.Roles.Admin.Should().Be("Admin");
        AppConstants.Roles.User.Should().Be("User");
        AppConstants.Providers.VidSrc.Should().Be("vidsrc");
        AppConstants.Providers.BunnyStream.Should().Be("bunny");
    }

    [Theory]
    [InlineData("TitleText", "title_text")]
    [InlineData("CreatedAt", "created_at")]
    [InlineData("SourceProviderId", "source_provider_id")]
    [InlineData("ApplicationUser", "application_user")]
    [InlineData("Id", "id")]
    public void StringExtensions_ToSnakeCase_ShouldConvertProperly(string input, string expected)
    {
        input.ToSnakeCase().Should().Be(expected);
    }

    [Fact]
    public void Title_Defaults_ShouldBeValid()
    {
        var title = new Title
        {
            TitleText = "Inception",
            TmdbId = 27205
        };

        title.Type.Should().Be("movie");
        title.IsFeatured.Should().BeFalse();
        title.ViewCount.Should().Be(0);
        title.OriginalLanguage.Should().Be("en");
    }

    [Fact]
    public void SourceProvider_Defaults_ShouldBeActive()
    {
        var provider = new SourceProvider
        {
            Name = "VidSrc",
            Slug = "vidsrc",
            Type = AppConstants.SourceProviderTypes.ThirdPartyEmbed
        };

        provider.IsActive.Should().BeTrue();
        provider.Priority.Should().Be(1);
    }
}
