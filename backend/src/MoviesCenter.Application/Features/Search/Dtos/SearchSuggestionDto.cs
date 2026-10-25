namespace MoviesCenter.Application.Features.Search.Dtos;

public record SearchSuggestionDto(
    int TmdbId,
    string Type,
    string Title,
    string? PosterPath,
    double Rating,
    string Year,
    List<string> Genres
);
