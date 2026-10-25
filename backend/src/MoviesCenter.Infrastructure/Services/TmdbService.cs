using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Home.Dtos;

namespace MoviesCenter.Infrastructure.Services;

public class TmdbService : ITmdbService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<TmdbService> _logger;
    private Dictionary<int, string>? _cachedGenreMap;
    private DateTime _genreMapExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _genreLock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TmdbService(HttpClient httpClient, IConfiguration configuration, ILogger<TmdbService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["TMDB_API_KEY"] 
            ?? configuration["Tmdb:ApiKey"] 
            ?? "4afe98c4e5ae2c05958c237fb4dca934";

        var baseUrl = configuration["Tmdb:BaseUrl"] ?? "https://api.themoviedb.org/3";
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(baseUrl);
        }
    }

    public async Task<List<MediaCardDto>> GetTrendingMoviesAsync(CancellationToken cancellationToken = default)
    {
        return await FetchListAsync<TmdbMovieResult>(
            $"trending/movie/week?api_key={_apiKey}",
            item => MapMovieToCard(item),
            cancellationToken
        );
    }

    public async Task<List<MediaCardDto>> GetTrendingTvAsync(CancellationToken cancellationToken = default)
    {
        return await FetchListAsync<TmdbTvResult>(
            $"trending/tv/week?api_key={_apiKey}",
            item => MapTvToCard(item),
            cancellationToken
        );
    }

    public async Task<List<MediaCardDto>> GetPopularMoviesAsync(CancellationToken cancellationToken = default)
    {
        return await FetchListAsync<TmdbMovieResult>(
            $"movie/popular?api_key={_apiKey}",
            item => MapMovieToCard(item),
            cancellationToken
        );
    }

    public async Task<List<MediaCardDto>> GetTopRatedMoviesAsync(CancellationToken cancellationToken = default)
    {
        return await FetchListAsync<TmdbMovieResult>(
            $"movie/top_rated?api_key={_apiKey}",
            item => MapMovieToCard(item),
            cancellationToken
        );
    }

    public async Task<List<MediaCardDto>> GetPopularTvAsync(CancellationToken cancellationToken = default)
    {
        return await FetchListAsync<TmdbTvResult>(
            $"tv/popular?api_key={_apiKey}",
            item => MapTvToCard(item),
            cancellationToken
        );
    }

    public async Task<List<MediaCardDto>> GetUpcomingMoviesAsync(CancellationToken cancellationToken = default)
    {
        return await FetchListAsync<TmdbMovieResult>(
            $"movie/upcoming?api_key={_apiKey}",
            item => MapMovieToCard(item),
            cancellationToken
        );
    }

    public async Task<HeroItemDto?> GetHeroDetailsAsync(int tmdbId, string type, CancellationToken cancellationToken = default)
    {
        try
        {
            if (type.Equals("series", StringComparison.OrdinalIgnoreCase))
            {
                var response = await _httpClient.GetFromJsonAsync<TmdbTvDetails>(
                    $"tv/{tmdbId}?api_key={_apiKey}",
                    JsonOptions,
                    cancellationToken
                );

                if (response == null) return null;

                var year = ExtractYear(response.FirstAirDate);
                return new HeroItemDto(
                    TmdbId: response.Id,
                    Type: "series",
                    Title: response.Name ?? "Featured Series",
                    Overview: response.Overview ?? string.Empty,
                    BackdropPath: response.BackdropPath,
                    PosterPath: response.PosterPath,
                    Rating: Math.Round(response.VoteAverage, 1),
                    ReleaseDate: response.FirstAirDate ?? string.Empty,
                    Year: year,
                    RuntimeMinutes: null,
                    TotalSeasons: response.NumberOfSeasons,
                    Genres: response.Genres.Select(g => g.Name).ToList()
                );
            }
            else
            {
                var response = await _httpClient.GetFromJsonAsync<TmdbMovieDetails>(
                    $"movie/{tmdbId}?api_key={_apiKey}",
                    JsonOptions,
                    cancellationToken
                );

                if (response == null) return null;

                var year = ExtractYear(response.ReleaseDate);
                return new HeroItemDto(
                    TmdbId: response.Id,
                    Type: "movie",
                    Title: response.Title ?? "Featured Movie",
                    Overview: response.Overview ?? string.Empty,
                    BackdropPath: response.BackdropPath,
                    PosterPath: response.PosterPath,
                    Rating: Math.Round(response.VoteAverage, 1),
                    ReleaseDate: response.ReleaseDate ?? string.Empty,
                    Year: year,
                    RuntimeMinutes: response.Runtime,
                    TotalSeasons: null,
                    Genres: response.Genres.Select(g => g.Name).ToList()
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load hero details for {Type} {TmdbId}", type, tmdbId);
            return null;
        }
    }

    public async Task<Dictionary<int, string>> GetGenreMapAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedGenreMap != null && DateTime.UtcNow < _genreMapExpiry)
        {
            return _cachedGenreMap;
        }

        await _genreLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedGenreMap != null && DateTime.UtcNow < _genreMapExpiry)
            {
                return _cachedGenreMap;
            }

            var map = new Dictionary<int, string>();

            try
            {
                var movieGenres = await _httpClient.GetFromJsonAsync<TmdbGenreListResponse>(
                    $"genre/movie/list?api_key={_apiKey}",
                    JsonOptions,
                    cancellationToken
                );
                if (movieGenres?.Genres != null)
                {
                    foreach (var g in movieGenres.Genres) map[g.Id] = g.Name;
                }

                var tvGenres = await _httpClient.GetFromJsonAsync<TmdbGenreListResponse>(
                    $"genre/tv/list?api_key={_apiKey}",
                    JsonOptions,
                    cancellationToken
                );
                if (tvGenres?.Genres != null)
                {
                    foreach (var g in tvGenres.Genres) map[g.Id] = g.Name;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load TMDB genres list; using fallback defaults.");
            }

            _cachedGenreMap = map;
            _genreMapExpiry = DateTime.UtcNow.AddHours(24);
            return map;
        }
        finally
        {
            _genreLock.Release();
        }
    }

    private async Task<List<MediaCardDto>> FetchListAsync<T>(
        string endpoint,
        Func<T, MediaCardDto> mapper,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<TmdbPagedResponse<T>>(
                endpoint,
                JsonOptions,
                cancellationToken
            );

            if (response?.Results == null)
            {
                return new List<MediaCardDto>();
            }

            return response.Results.Select(mapper).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling TMDB endpoint: {Endpoint}", endpoint);
            return new List<MediaCardDto>();
        }
    }

    private MediaCardDto MapMovieToCard(TmdbMovieResult movie)
    {
        var year = ExtractYear(movie.ReleaseDate);
        var genreNames = MapGenreIds(movie.GenreIds);

        return new MediaCardDto(
            TmdbId: movie.Id,
            Type: "movie",
            Title: movie.Title ?? "Untitled Movie",
            PosterPath: movie.PosterPath,
            BackdropPath: movie.BackdropPath,
            Rating: Math.Round(movie.VoteAverage, 1),
            Year: year,
            Genres: genreNames
        );
    }

    private MediaCardDto MapTvToCard(TmdbTvResult tv)
    {
        var year = ExtractYear(tv.FirstAirDate);
        var genreNames = MapGenreIds(tv.GenreIds);

        return new MediaCardDto(
            TmdbId: tv.Id,
            Type: "series",
            Title: tv.Name ?? "Untitled Series",
            PosterPath: tv.PosterPath,
            BackdropPath: tv.BackdropPath,
            Rating: Math.Round(tv.VoteAverage, 1),
            Year: year,
            Genres: genreNames
        );
    }

    private static string ExtractYear(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString) || dateString.Length < 4)
            return string.Empty;

        return dateString[..4];
    }

    private List<string> MapGenreIds(List<int>? genreIds)
    {
        if (genreIds == null || genreIds.Count == 0) return new List<string>();

        var map = _cachedGenreMap;
        if (map == null || map.Count == 0) return new List<string>();

        return genreIds
            .Where(id => map.ContainsKey(id))
            .Select(id => map[id])
            .Take(3)
            .ToList();
    }

    #region TMDB Contract Records
    private class TmdbPagedResponse<T>
    {
        [JsonPropertyName("results")]
        public List<T> Results { get; set; } = new();
    }

    private class TmdbMovieResult
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Overview { get; set; }
        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }
        [JsonPropertyName("backdrop_path")]
        public string? BackdropPath { get; set; }
        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; set; }
        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }
        [JsonPropertyName("genre_ids")]
        public List<int> GenreIds { get; set; } = new();
    }

    private class TmdbTvResult
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Overview { get; set; }
        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }
        [JsonPropertyName("backdrop_path")]
        public string? BackdropPath { get; set; }
        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; set; }
        [JsonPropertyName("first_air_date")]
        public string? FirstAirDate { get; set; }
        [JsonPropertyName("genre_ids")]
        public List<int> GenreIds { get; set; } = new();
    }

    private class TmdbMovieDetails
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Overview { get; set; }
        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }
        [JsonPropertyName("backdrop_path")]
        public string? BackdropPath { get; set; }
        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; set; }
        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }
        public int? Runtime { get; set; }
        public List<TmdbGenreItem> Genres { get; set; } = new();
    }

    private class TmdbTvDetails
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Overview { get; set; }
        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }
        [JsonPropertyName("backdrop_path")]
        public string? BackdropPath { get; set; }
        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; set; }
        [JsonPropertyName("first_air_date")]
        public string? FirstAirDate { get; set; }
        [JsonPropertyName("number_of_seasons")]
        public int? NumberOfSeasons { get; set; }
        public List<TmdbGenreItem> Genres { get; set; } = new();
    }

    private class TmdbGenreListResponse
    {
        public List<TmdbGenreItem> Genres { get; set; } = new();
    }

    private class TmdbGenreItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
    #endregion
}
