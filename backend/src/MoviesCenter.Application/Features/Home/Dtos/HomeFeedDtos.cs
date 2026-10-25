namespace MoviesCenter.Application.Features.Home.Dtos;

public record HomeFeedDto(
    HeroItemDto Hero,
    List<ContentRowDto> Rows
);

public record HeroItemDto(
    int TmdbId,
    string Type, // "movie" or "series"
    string Title,
    string Overview,
    string? BackdropPath,
    string? PosterPath,
    double Rating,
    string ReleaseDate,
    string Year,
    int? RuntimeMinutes,
    int? TotalSeasons,
    List<string> Genres,
    string Quality = "4K Ultra HD"
);

public record ContentRowDto(
    string Id,
    string Title,
    string Type,
    string ViewAllRoute,
    List<MediaCardDto> Items
);

public record MediaCardDto(
    int TmdbId,
    string Type, // "movie" or "series"
    string Title,
    string? PosterPath,
    string? BackdropPath,
    double Rating,
    string Year,
    List<string> Genres
);
