namespace MoviesCenter.Application.Features.Catalog.Dtos;

public record CastMemberDto(
    int PersonId,
    int TmdbId,
    string Name,
    string Character,
    string? ProfilePath,
    int DisplayOrder
);

public record VideoDto(
    int Id,
    string Key,
    string Name,
    string Type,
    string Site
);

public record WatchProviderDto(
    int ProviderId,
    int TmdbId,
    string Name,
    string? LogoPath,
    string Type,
    string Country
);

public record EpisodeDto(
    int Id,
    int TmdbId,
    int EpisodeNumber,
    string Name,
    string? Overview,
    string? StillPath,
    string? AirDate,
    int? RuntimeMinutes
);

public record SeasonDto(
    int Id,
    int TmdbId,
    int SeasonNumber,
    string Name,
    string? Overview,
    string? PosterPath,
    string? AirDate,
    int EpisodeCount,
    List<EpisodeDto> Episodes
);

public record TitleDetailDto(
    int Id,
    int TmdbId,
    string Type,
    string Title,
    string? OriginalTitle,
    string? Tagline,
    string? Overview,
    string? PosterPath,
    string? BackdropPath,
    double Rating,
    int VoteCount,
    string? ReleaseDate,
    string Year,
    int? RuntimeMinutes,
    string? Status,
    string? ContentRating,
    string OriginalLanguage,
    int? TotalSeasons,
    int? TotalEpisodes,
    List<string> Genres,
    List<CastMemberDto> Cast,
    List<VideoDto> Videos,
    List<WatchProviderDto> Providers,
    List<SeasonDto> Seasons
);
