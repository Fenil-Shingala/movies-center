namespace MoviesCenter.Application.Common.Models;

public record TmdbItemSummary(
    int Id,
    string Type, // "movie" or "series"
    string Title,
    string? OriginalTitle,
    string? Overview,
    string? PosterPath,
    string? BackdropPath,
    string? ReleaseDate,
    double VoteAverage,
    int VoteCount,
    double Popularity,
    string OriginalLanguage,
    List<int> GenreIds
);

public record TmdbCastMember(
    int PersonTmdbId,
    string Name,
    string? ProfilePath,
    string Character,
    int DisplayOrder
);

public record TmdbVideoItem(
    string Key,
    string Name,
    string Type,
    string Site
);

public record TmdbProviderItem(
    int ProviderTmdbId,
    string Name,
    string? LogoPath,
    string Type,
    string Country
);

public record TmdbSeasonSummary(
    int TmdbId,
    int SeasonNumber,
    string Name,
    string? Overview,
    string? PosterPath,
    string? AirDate,
    int EpisodeCount
);

public record TmdbEpisodeSummary(
    int TmdbId,
    int EpisodeNumber,
    string Name,
    string? Overview,
    string? StillPath,
    string? AirDate,
    int? RuntimeMinutes
);

public record TmdbFullDetail(
    int TmdbId,
    string Type, // "movie" or "series"
    string Title,
    string? OriginalTitle,
    string? Overview,
    string? Tagline,
    string? PosterPath,
    string? BackdropPath,
    string? ReleaseDate,
    string? FirstAirDate,
    int? RuntimeMinutes,
    int? TotalSeasons,
    int? TotalEpisodes,
    double VoteAverage,
    int VoteCount,
    double Popularity,
    string OriginalLanguage,
    string? Status,
    List<int> GenreIds,
    List<TmdbCastMember> Cast,
    List<TmdbVideoItem> Videos,
    List<TmdbProviderItem> Providers,
    List<TmdbSeasonSummary> Seasons
);
