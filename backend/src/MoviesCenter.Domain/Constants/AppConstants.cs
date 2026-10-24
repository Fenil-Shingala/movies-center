namespace MoviesCenter.Domain.Constants;

public static class AppConstants
{
    public const string AppName = "Movies Center";
    public const string AppVersion = "1.0.0";

    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }

    public static class TitleTypes
    {
        public const string Movie = "movie";
        public const string Series = "series";
    }

    public static class SourceProviderTypes
    {
        public const string ThirdPartyEmbed = "ThirdPartyEmbed";
        public const string Upload = "Upload";
    }

    public static class Providers
    {
        public const string VidSrc = "vidsrc";
        public const string BunnyStream = "bunny";
    }
}
