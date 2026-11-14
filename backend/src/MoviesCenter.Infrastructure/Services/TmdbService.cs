using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Common.Models;
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

        var baseUrl = configuration["Tmdb:BaseUrl"] ?? "https://api.themoviedb.org/3/";
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(baseUrl);
        }
    }

    #region Direct / Legacy Fetchers
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
    #endregion

    #region Sync Endpoints
    public async Task<List<TmdbItemSummary>> GetListEndpointAsync(string endpoint, string type, CancellationToken cancellationToken = default)
    {
        var separator = endpoint.Contains('?') ? "&" : "?";
        var url = $"{endpoint}{separator}api_key={_apiKey}";

        try
        {
            if (type.Equals("series", StringComparison.OrdinalIgnoreCase))
            {
                var response = await _httpClient.GetFromJsonAsync<TmdbPagedResponse<TmdbTvResult>>(url, JsonOptions, cancellationToken);
                if (response?.Results == null) return [];

                return response.Results.Select(tv => new TmdbItemSummary(
                    Id: tv.Id,
                    Type: "series",
                    Title: tv.Name ?? "Untitled Series",
                    OriginalTitle: null,
                    Overview: tv.Overview,
                    PosterPath: tv.PosterPath,
                    BackdropPath: tv.BackdropPath,
                    ReleaseDate: tv.FirstAirDate,
                    VoteAverage: tv.VoteAverage,
                    VoteCount: tv.VoteCount,
                    Popularity: tv.Popularity,
                    OriginalLanguage: tv.OriginalLanguage ?? "en",
                    GenreIds: tv.GenreIds
                )).ToList();
            }
            else
            {
                var response = await _httpClient.GetFromJsonAsync<TmdbPagedResponse<TmdbMovieResult>>(url, JsonOptions, cancellationToken);
                if (response?.Results == null) return [];

                return response.Results.Select(m => new TmdbItemSummary(
                    Id: m.Id,
                    Type: "movie",
                    Title: m.Title ?? "Untitled Movie",
                    OriginalTitle: m.OriginalTitle,
                    Overview: m.Overview,
                    PosterPath: m.PosterPath,
                    BackdropPath: m.BackdropPath,
                    ReleaseDate: m.ReleaseDate,
                    VoteAverage: m.VoteAverage,
                    VoteCount: m.VoteCount,
                    Popularity: m.Popularity,
                    OriginalLanguage: m.OriginalLanguage ?? "en",
                    GenreIds: m.GenreIds
                )).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch list endpoint {Url}", url);
            return [];
        }
    }

    public async Task<TmdbFullDetail?> GetFullDetailsAsync(int tmdbId, string type, CancellationToken cancellationToken = default)
    {
        var isSeries = type.Equals("series", StringComparison.OrdinalIgnoreCase);
        var path = isSeries ? $"tv/{tmdbId}" : $"movie/{tmdbId}";
        var url = $"{path}?api_key={_apiKey}&append_to_response=credits,videos,watch/providers";

        try
        {
            if (isSeries)
            {
                var response = await _httpClient.GetFromJsonAsync<TmdbTvFullResponse>(url, JsonOptions, cancellationToken);
                if (response == null) return null;

                var cast = response.Credits?.Cast?.Take(15).Select(c => new TmdbCastMember(
                    PersonTmdbId: c.Id,
                    Name: c.Name ?? string.Empty,
                    ProfilePath: c.ProfilePath,
                    Character: c.Character ?? string.Empty,
                    DisplayOrder: c.Order
                )).ToList() ?? [];

                var videos = response.Videos?.Results?.Where(v => v.Site == "YouTube")
                    .Select(v => new TmdbVideoItem(v.Key ?? string.Empty, v.Name ?? string.Empty, v.Type ?? "Trailer", v.Site ?? "YouTube"))
                    .ToList() ?? [];

                var providers = ExtractProviders(response.WatchProviders);

                var seasons = response.Seasons?.Select(s => new TmdbSeasonSummary(
                    TmdbId: s.Id,
                    SeasonNumber: s.SeasonNumber,
                    Name: s.Name ?? $"Season {s.SeasonNumber}",
                    Overview: s.Overview,
                    PosterPath: s.PosterPath,
                    AirDate: s.AirDate,
                    EpisodeCount: s.EpisodeCount
                )).ToList() ?? [];

                return new TmdbFullDetail(
                    TmdbId: response.Id,
                    Type: "series",
                    Title: response.Name ?? "Untitled Series",
                    OriginalTitle: response.OriginalName,
                    Overview: response.Overview,
                    Tagline: response.Tagline,
                    PosterPath: response.PosterPath,
                    BackdropPath: response.BackdropPath,
                    ReleaseDate: null,
                    FirstAirDate: response.FirstAirDate,
                    RuntimeMinutes: null,
                    TotalSeasons: response.NumberOfSeasons,
                    TotalEpisodes: response.NumberOfEpisodes,
                    VoteAverage: response.VoteAverage,
                    VoteCount: response.VoteCount,
                    Popularity: response.Popularity,
                    OriginalLanguage: response.OriginalLanguage ?? "en",
                    Status: response.Status,
                    GenreIds: response.Genres.Select(g => g.Id).ToList(),
                    Cast: cast,
                    Videos: videos,
                    Providers: providers,
                    Seasons: seasons
                );
            }
            else
            {
                var response = await _httpClient.GetFromJsonAsync<TmdbMovieFullResponse>(url, JsonOptions, cancellationToken);
                if (response == null) return null;

                var cast = response.Credits?.Cast?.Take(15).Select(c => new TmdbCastMember(
                    PersonTmdbId: c.Id,
                    Name: c.Name ?? string.Empty,
                    ProfilePath: c.ProfilePath,
                    Character: c.Character ?? string.Empty,
                    DisplayOrder: c.Order
                )).ToList() ?? [];

                var videos = response.Videos?.Results?.Where(v => v.Site == "YouTube")
                    .Select(v => new TmdbVideoItem(v.Key ?? string.Empty, v.Name ?? string.Empty, v.Type ?? "Trailer", v.Site ?? "YouTube"))
                    .ToList() ?? [];

                var providers = ExtractProviders(response.WatchProviders);

                return new TmdbFullDetail(
                    TmdbId: response.Id,
                    Type: "movie",
                    Title: response.Title ?? "Untitled Movie",
                    OriginalTitle: response.OriginalTitle,
                    Overview: response.Overview,
                    Tagline: response.Tagline,
                    PosterPath: response.PosterPath,
                    BackdropPath: response.BackdropPath,
                    ReleaseDate: response.ReleaseDate,
                    FirstAirDate: null,
                    RuntimeMinutes: response.Runtime,
                    TotalSeasons: null,
                    TotalEpisodes: null,
                    VoteAverage: response.VoteAverage,
                    VoteCount: response.VoteCount,
                    Popularity: response.Popularity,
                    OriginalLanguage: response.OriginalLanguage ?? "en",
                    Status: response.Status,
                    GenreIds: response.Genres.Select(g => g.Id).ToList(),
                    Cast: cast,
                    Videos: videos,
                    Providers: providers,
                    Seasons: []
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load full TMDB details for {Type} {TmdbId}", type, tmdbId);
            return null;
        }
    }

    public async Task<List<TmdbEpisodeSummary>> GetSeasonEpisodesAsync(int tmdbId, int seasonNumber, CancellationToken cancellationToken = default)
    {
        var url = $"tv/{tmdbId}/season/{seasonNumber}?api_key={_apiKey}";
        try
        {
            var response = await _httpClient.GetFromJsonAsync<TmdbSeasonEpisodesResponse>(url, JsonOptions, cancellationToken);
            if (response?.Episodes == null) return [];

            return response.Episodes.Select(e => new TmdbEpisodeSummary(
                TmdbId: e.Id,
                EpisodeNumber: e.EpisodeNumber,
                Name: e.Name ?? $"Episode {e.EpisodeNumber}",
                Overview: e.Overview,
                StillPath: e.StillPath,
                AirDate: e.AirDate,
                RuntimeMinutes: e.Runtime
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch episodes for TV {TmdbId} Season {SeasonNumber}", tmdbId, seasonNumber);
            return [];
        }
    }

    public async Task<List<MoviesCenter.Application.Features.Admin.Dtos.TmdbSearchResultItemDto>> SearchTmdbAsync(string query, string? type = null, string? searchBy = null, CancellationToken cancellationToken = default)
    {
        var results = new List<MoviesCenter.Application.Features.Admin.Dtos.TmdbSearchResultItemDto>();
        if (string.IsNullOrWhiteSpace(query)) return results;

        var encodedQuery = Uri.EscapeDataString(query.Trim());
        Dictionary<int, string> genreMap = [];
        try
        {
            genreMap = await GetGenreMapAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load genre map for TMDB search");
        }

        var searchMovies = string.IsNullOrWhiteSpace(type) || 
                           string.Equals(type, "all", StringComparison.OrdinalIgnoreCase) || 
                           string.Equals(type, "movie", StringComparison.OrdinalIgnoreCase);

        var searchTv = string.IsNullOrWhiteSpace(type) || 
                       string.Equals(type, "all", StringComparison.OrdinalIgnoreCase) || 
                       string.Equals(type, "series", StringComparison.OrdinalIgnoreCase) || 
                       string.Equals(type, "tv", StringComparison.OrdinalIgnoreCase);

        var searchByNorm = string.IsNullOrWhiteSpace(searchBy) ? "all" : searchBy.Trim().ToLowerInvariant();
        var searchTitle = searchByNorm == "all" || searchByNorm == "title";
        var searchCast = searchByNorm == "all" || searchByNorm == "cast" || searchByNorm == "actor";

        if (searchTitle)
        {
            if (searchMovies)
            {
                try
                {
                    var movieUrl = $"search/movie?api_key={_apiKey}&query={encodedQuery}&page=1&include_adult=false";
                    var movieResp = await _httpClient.GetFromJsonAsync<TmdbPagedResponse<TmdbMovieResult>>(movieUrl, JsonOptions, cancellationToken);
                    if (movieResp?.Results != null)
                    {
                        var movieTake = searchTv ? 15 : 24;
                        results.AddRange(movieResp.Results
                            .Where(m => !string.IsNullOrWhiteSpace(m.Title))
                            .Take(movieTake)
                            .Select(m => {
                                var genres = m.GenreIds
                                    .Select(gid => genreMap.TryGetValue(gid, out var gname) ? gname : null)
                                    .Where(g => !string.IsNullOrEmpty(g))
                                    .Cast<string>()
                                    .Take(3)
                                    .ToList();

                                return new MoviesCenter.Application.Features.Admin.Dtos.TmdbSearchResultItemDto(
                                    TmdbId: m.Id,
                                    Title: m.Title ?? "Untitled Movie",
                                    Type: "movie",
                                    PosterPath: m.PosterPath,
                                    BackdropPath: m.BackdropPath,
                                    ReleaseDate: m.ReleaseDate,
                                    Year: ExtractYear(m.ReleaseDate),
                                    Rating: Math.Round(m.VoteAverage, 1),
                                    Overview: m.Overview,
                                    Genres: genres,
                                    Popularity: m.Popularity
                                );
                            }));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to search TMDB movies for query {Query}", query);
                }
            }

            if (searchTv)
            {
                try
                {
                    var tvUrl = $"search/tv?api_key={_apiKey}&query={encodedQuery}&page=1&include_adult=false";
                    var tvResp = await _httpClient.GetFromJsonAsync<TmdbPagedResponse<TmdbTvResult>>(tvUrl, JsonOptions, cancellationToken);
                    if (tvResp?.Results != null)
                    {
                        var tvTake = searchMovies ? 15 : 24;
                        results.AddRange(tvResp.Results
                            .Where(t => !string.IsNullOrWhiteSpace(t.Name))
                            .Take(tvTake)
                            .Select(t => {
                                var genres = t.GenreIds
                                    .Select(gid => genreMap.TryGetValue(gid, out var gname) ? gname : null)
                                    .Where(g => !string.IsNullOrEmpty(g))
                                    .Cast<string>()
                                    .Take(3)
                                    .ToList();

                                return new MoviesCenter.Application.Features.Admin.Dtos.TmdbSearchResultItemDto(
                                    TmdbId: t.Id,
                                    Title: t.Name ?? "Untitled Series",
                                    Type: "series",
                                    PosterPath: t.PosterPath,
                                    BackdropPath: t.BackdropPath,
                                    ReleaseDate: t.FirstAirDate,
                                    Year: ExtractYear(t.FirstAirDate),
                                    Rating: Math.Round(t.VoteAverage, 1),
                                    Overview: t.Overview,
                                    Genres: genres,
                                    Popularity: t.Popularity
                                );
                            }));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to search TMDB series for query {Query}", query);
                }
            }
        }

        if (searchCast)
        {
            try
            {
                var personUrl = $"search/person?api_key={_apiKey}&query={encodedQuery}&page=1&include_adult=false";
                var personResp = await _httpClient.GetFromJsonAsync<TmdbPersonSearchResponse>(personUrl, JsonOptions, cancellationToken);
                if (personResp?.Results != null && personResp.Results.Count > 0)
                {
                    var matchedPersons = personResp.Results
                        .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                        .OrderByDescending(p => p.KnownForDepartment == "Acting")
                        .ThenByDescending(p => p.Popularity)
                        .Take(2)
                        .ToList();

                    foreach (var person in matchedPersons)
                    {
                        var creditsUrl = $"person/{person.Id}/combined_credits?api_key={_apiKey}";
                        var creditsResp = await _httpClient.GetFromJsonAsync<TmdbPersonCombinedCreditsResponse>(creditsUrl, JsonOptions, cancellationToken);
                        if (creditsResp?.Cast != null)
                        {
                            var castItems = creditsResp.Cast
                                .Where(c => (searchMovies && c.MediaType == "movie") || (searchTv && c.MediaType == "tv"))
                                .OrderByDescending(c => c.Popularity)
                                .Take(20)
                                .Select(c => {
                                    var itemType = c.MediaType == "tv" ? "series" : "movie";
                                    var itemTitle = itemType == "series" ? (c.Name ?? "Untitled Series") : (c.Title ?? "Untitled Movie");
                                    var date = itemType == "series" ? c.FirstAirDate : c.ReleaseDate;
                                    var genres = (c.GenreIds ?? [])
                                        .Select(gid => genreMap.TryGetValue(gid, out var gname) ? gname : null)
                                        .Where(g => !string.IsNullOrEmpty(g))
                                        .Cast<string>()
                                        .Take(3)
                                        .ToList();

                                    return new MoviesCenter.Application.Features.Admin.Dtos.TmdbSearchResultItemDto(
                                        TmdbId: c.Id,
                                        Title: itemTitle,
                                        Type: itemType,
                                        PosterPath: c.PosterPath,
                                        BackdropPath: c.BackdropPath,
                                        ReleaseDate: date,
                                        Year: ExtractYear(date),
                                        Rating: Math.Round(c.VoteAverage, 1),
                                        Overview: c.Overview,
                                        Genres: genres,
                                        Popularity: c.Popularity
                                    );
                                });

                            results.AddRange(castItems);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to search TMDB cast/person credits for query {Query}", query);
            }
        }

        return results
            .DistinctBy(r => r.TmdbId)
            .OrderByDescending(r => r.Popularity)
            .ThenByDescending(r => r.Rating)
            .ToList();
    }

    public async Task<List<MoviesCenter.Application.Features.Admin.Dtos.TmdbSearchResultItemDto>> GetRecommendationsAsync(int tmdbId, string type, CancellationToken cancellationToken = default)
    {
        var results = new List<MoviesCenter.Application.Features.Admin.Dtos.TmdbSearchResultItemDto>();
        if (tmdbId <= 0) return results;

        Dictionary<int, string> genreMap = [];
        try
        {
            genreMap = await GetGenreMapAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load genre map for TMDB recommendations");
        }

        var isTv = string.Equals(type, "series", StringComparison.OrdinalIgnoreCase) || 
                   string.Equals(type, "tv", StringComparison.OrdinalIgnoreCase);
        var mediaEndpoint = isTv ? "tv" : "movie";

        try
        {
            // 1. Primary: Official TMDB Recommendations algorithm
            var recsUrl = $"{mediaEndpoint}/{tmdbId}/recommendations?api_key={_apiKey}&page=1";
            if (isTv)
            {
                var tvResp = await _httpClient.GetFromJsonAsync<TmdbPagedResponse<TmdbTvResult>>(recsUrl, JsonOptions, cancellationToken);
                if (tvResp?.Results != null && tvResp.Results.Count > 0)
                {
                    results.AddRange(tvResp.Results
                        .Where(t => !string.IsNullOrWhiteSpace(t.Name))
                        .Select(t => {
                            var genres = t.GenreIds
                                .Select(gid => genreMap.TryGetValue(gid, out var gname) ? gname : null)
                                .Where(g => !string.IsNullOrEmpty(g))
                                .Cast<string>()
                                .Take(3)
                                .ToList();

                            return new MoviesCenter.Application.Features.Admin.Dtos.TmdbSearchResultItemDto(
                                TmdbId: t.Id,
                                Title: t.Name ?? "Untitled Series",
                                Type: "series",
                                PosterPath: t.PosterPath,
                                BackdropPath: t.BackdropPath,
                                ReleaseDate: t.FirstAirDate,
                                Year: ExtractYear(t.FirstAirDate),
                                Rating: Math.Round(t.VoteAverage, 1),
                                Overview: t.Overview,
                                Genres: genres,
                                Popularity: t.Popularity
                            );
                        }));
                }
            }
            else
            {
                var movieResp = await _httpClient.GetFromJsonAsync<TmdbPagedResponse<TmdbMovieResult>>(recsUrl, JsonOptions, cancellationToken);
                if (movieResp?.Results != null && movieResp.Results.Count > 0)
                {
                    results.AddRange(movieResp.Results
                        .Where(m => !string.IsNullOrWhiteSpace(m.Title))
                        .Select(m => {
                            var genres = m.GenreIds
                                .Select(gid => genreMap.TryGetValue(gid, out var gname) ? gname : null)
                                .Where(g => !string.IsNullOrEmpty(g))
                                .Cast<string>()
                                .Take(3)
                                .ToList();

                            return new MoviesCenter.Application.Features.Admin.Dtos.TmdbSearchResultItemDto(
                                TmdbId: m.Id,
                                Title: m.Title ?? "Untitled Movie",
                                Type: "movie",
                                PosterPath: m.PosterPath,
                                BackdropPath: m.BackdropPath,
                                ReleaseDate: m.ReleaseDate,
                                Year: ExtractYear(m.ReleaseDate),
                                Rating: Math.Round(m.VoteAverage, 1),
                                Overview: m.Overview,
                                Genres: genres,
                                Popularity: m.Popularity
                            );
                        }));
                }
            }

            // 2. Fallback to similar endpoint if recommendations returned fewer than 6
            if (results.Count < 6)
            {
                var similarUrl = $"{mediaEndpoint}/{tmdbId}/similar?api_key={_apiKey}&page=1";
                if (isTv)
                {
                    var tvResp = await _httpClient.GetFromJsonAsync<TmdbPagedResponse<TmdbTvResult>>(similarUrl, JsonOptions, cancellationToken);
                    if (tvResp?.Results != null)
                    {
                        var existingIds = results.Select(r => r.TmdbId).ToHashSet();
                        results.AddRange(tvResp.Results
                            .Where(t => !string.IsNullOrWhiteSpace(t.Name) && !existingIds.Contains(t.Id))
                            .Select(t => {
                                var genres = t.GenreIds
                                    .Select(gid => genreMap.TryGetValue(gid, out var gname) ? gname : null)
                                    .Where(g => !string.IsNullOrEmpty(g))
                                    .Cast<string>()
                                    .Take(3)
                                    .ToList();

                                return new MoviesCenter.Application.Features.Admin.Dtos.TmdbSearchResultItemDto(
                                    TmdbId: t.Id,
                                    Title: t.Name ?? "Untitled Series",
                                    Type: "series",
                                    PosterPath: t.PosterPath,
                                    BackdropPath: t.BackdropPath,
                                    ReleaseDate: t.FirstAirDate,
                                    Year: ExtractYear(t.FirstAirDate),
                                    Rating: Math.Round(t.VoteAverage, 1),
                                    Overview: t.Overview,
                                    Genres: genres,
                                    Popularity: t.Popularity
                                );
                            }));
                    }
                }
                else
                {
                    var movieResp = await _httpClient.GetFromJsonAsync<TmdbPagedResponse<TmdbMovieResult>>(similarUrl, JsonOptions, cancellationToken);
                    if (movieResp?.Results != null)
                    {
                        var existingIds = results.Select(r => r.TmdbId).ToHashSet();
                        results.AddRange(movieResp.Results
                            .Where(m => !string.IsNullOrWhiteSpace(m.Title) && !existingIds.Contains(m.Id))
                            .Select(m => {
                                var genres = m.GenreIds
                                    .Select(gid => genreMap.TryGetValue(gid, out var gname) ? gname : null)
                                    .Where(g => !string.IsNullOrEmpty(g))
                                    .Cast<string>()
                                    .Take(3)
                                    .ToList();

                                return new MoviesCenter.Application.Features.Admin.Dtos.TmdbSearchResultItemDto(
                                    TmdbId: m.Id,
                                    Title: m.Title ?? "Untitled Movie",
                                    Type: "movie",
                                    PosterPath: m.PosterPath,
                                    BackdropPath: m.BackdropPath,
                                    ReleaseDate: m.ReleaseDate,
                                    Year: ExtractYear(m.ReleaseDate),
                                    Rating: Math.Round(m.VoteAverage, 1),
                                    Overview: m.Overview,
                                    Genres: genres,
                                    Popularity: m.Popularity
                                );
                            }));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch TMDB recommendations for {MediaEndpoint} {TmdbId}", mediaEndpoint, tmdbId);
        }

        return results
            .DistinctBy(r => r.TmdbId)
            .ToList();
    }

    private static List<TmdbProviderItem> ExtractProviders(TmdbWatchProvidersResponse? wp)
    {
        if (wp?.Results == null || !wp.Results.TryGetValue("US", out var usProviders))
            return [];

        var list = new List<TmdbProviderItem>();

        if (usProviders.Flatrate != null)
        {
            foreach (var p in usProviders.Flatrate)
                list.Add(new TmdbProviderItem(p.ProviderId, p.ProviderName ?? string.Empty, p.LogoPath, "stream", "US"));
        }

        if (usProviders.Rent != null)
        {
            foreach (var p in usProviders.Rent)
                list.Add(new TmdbProviderItem(p.ProviderId, p.ProviderName ?? string.Empty, p.LogoPath, "rent", "US"));
        }

        if (usProviders.Buy != null)
        {
            foreach (var p in usProviders.Buy)
                list.Add(new TmdbProviderItem(p.ProviderId, p.ProviderName ?? string.Empty, p.LogoPath, "buy", "US"));
        }

        return list;
    }
    #endregion

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
                return [];
            }

            return response.Results.Select(mapper).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling TMDB endpoint: {Endpoint}", endpoint);
            return [];
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
        if (genreIds == null || genreIds.Count == 0) return [];

        var map = _cachedGenreMap;
        if (map == null || map.Count == 0) return [];

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
        public List<T> Results { get; set; } = [];
    }

    private class TmdbMovieResult
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        [JsonPropertyName("original_title")]
        public string? OriginalTitle { get; set; }
        public string? Overview { get; set; }
        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }
        [JsonPropertyName("backdrop_path")]
        public string? BackdropPath { get; set; }
        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; set; }
        [JsonPropertyName("vote_count")]
        public int VoteCount { get; set; }
        public double Popularity { get; set; }
        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }
        [JsonPropertyName("original_language")]
        public string? OriginalLanguage { get; set; }
        [JsonPropertyName("genre_ids")]
        public List<int> GenreIds { get; set; } = [];
    }

    private class TmdbTvResult
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        [JsonPropertyName("original_name")]
        public string? OriginalName { get; set; }
        public string? Overview { get; set; }
        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }
        [JsonPropertyName("backdrop_path")]
        public string? BackdropPath { get; set; }
        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; set; }
        [JsonPropertyName("vote_count")]
        public int VoteCount { get; set; }
        public double Popularity { get; set; }
        [JsonPropertyName("first_air_date")]
        public string? FirstAirDate { get; set; }
        [JsonPropertyName("original_language")]
        public string? OriginalLanguage { get; set; }
        [JsonPropertyName("genre_ids")]
        public List<int> GenreIds { get; set; } = [];
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
        public List<TmdbGenreItem> Genres { get; set; } = [];
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
        public List<TmdbGenreItem> Genres { get; set; } = [];
    }

    private class TmdbMovieFullResponse
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        [JsonPropertyName("original_title")]
        public string? OriginalTitle { get; set; }
        public string? Overview { get; set; }
        public string? Tagline { get; set; }
        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }
        [JsonPropertyName("backdrop_path")]
        public string? BackdropPath { get; set; }
        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }
        public int? Runtime { get; set; }
        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; set; }
        [JsonPropertyName("vote_count")]
        public int VoteCount { get; set; }
        public double Popularity { get; set; }
        [JsonPropertyName("original_language")]
        public string? OriginalLanguage { get; set; }
        public string? Status { get; set; }
        public List<TmdbGenreItem> Genres { get; set; } = [];
        public TmdbCreditsResponse? Credits { get; set; }
        public TmdbVideosResponse? Videos { get; set; }
        [JsonPropertyName("watch/providers")]
        public TmdbWatchProvidersResponse? WatchProviders { get; set; }
    }

    private class TmdbTvFullResponse
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        [JsonPropertyName("original_name")]
        public string? OriginalName { get; set; }
        public string? Overview { get; set; }
        public string? Tagline { get; set; }
        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }
        [JsonPropertyName("backdrop_path")]
        public string? BackdropPath { get; set; }
        [JsonPropertyName("first_air_date")]
        public string? FirstAirDate { get; set; }
        [JsonPropertyName("number_of_seasons")]
        public int? NumberOfSeasons { get; set; }
        [JsonPropertyName("number_of_episodes")]
        public int? NumberOfEpisodes { get; set; }
        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; set; }
        [JsonPropertyName("vote_count")]
        public int VoteCount { get; set; }
        public double Popularity { get; set; }
        [JsonPropertyName("original_language")]
        public string? OriginalLanguage { get; set; }
        public string? Status { get; set; }
        public List<TmdbGenreItem> Genres { get; set; } = [];
        public List<TmdbSeasonItem> Seasons { get; set; } = [];
        public TmdbCreditsResponse? Credits { get; set; }
        public TmdbVideosResponse? Videos { get; set; }
        [JsonPropertyName("watch/providers")]
        public TmdbWatchProvidersResponse? WatchProviders { get; set; }
    }

    private class TmdbCreditsResponse
    {
        public List<TmdbCastItem>? Cast { get; set; }
    }

    private class TmdbCastItem
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        [JsonPropertyName("profile_path")]
        public string? ProfilePath { get; set; }
        public string? Character { get; set; }
        public int Order { get; set; }
    }

    private class TmdbVideosResponse
    {
        public List<TmdbVideoRawItem>? Results { get; set; }
    }

    private class TmdbVideoRawItem
    {
        public string? Key { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Site { get; set; }
    }

    private class TmdbWatchProvidersResponse
    {
        public Dictionary<string, TmdbCountryProviders>? Results { get; set; }
    }

    private class TmdbCountryProviders
    {
        public List<TmdbProviderRawItem>? Flatrate { get; set; }
        public List<TmdbProviderRawItem>? Rent { get; set; }
        public List<TmdbProviderRawItem>? Buy { get; set; }
    }

    private class TmdbProviderRawItem
    {
        [JsonPropertyName("provider_id")]
        public int ProviderId { get; set; }
        [JsonPropertyName("provider_name")]
        public string? ProviderName { get; set; }
        [JsonPropertyName("logo_path")]
        public string? LogoPath { get; set; }
    }

    private class TmdbSeasonItem
    {
        public int Id { get; set; }
        [JsonPropertyName("season_number")]
        public int SeasonNumber { get; set; }
        public string? Name { get; set; }
        public string? Overview { get; set; }
        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }
        [JsonPropertyName("air_date")]
        public string? AirDate { get; set; }
        [JsonPropertyName("episode_count")]
        public int EpisodeCount { get; set; }
    }

    private class TmdbSeasonEpisodesResponse
    {
        public List<TmdbEpisodeRawItem>? Episodes { get; set; }
    }

    private class TmdbEpisodeRawItem
    {
        public int Id { get; set; }
        [JsonPropertyName("episode_number")]
        public int EpisodeNumber { get; set; }
        public string? Name { get; set; }
        public string? Overview { get; set; }
        [JsonPropertyName("still_path")]
        public string? StillPath { get; set; }
        [JsonPropertyName("air_date")]
        public string? AirDate { get; set; }
        public int? Runtime { get; set; }
    }

    private class TmdbGenreListResponse
    {
        public List<TmdbGenreItem> Genres { get; set; } = [];
    }

    private class TmdbGenreItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class TmdbPersonSearchResponse
    {
        public List<TmdbPersonResult>? Results { get; set; }
    }

    private class TmdbPersonResult
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        [JsonPropertyName("known_for_department")]
        public string? KnownForDepartment { get; set; }
        public double Popularity { get; set; }
        [JsonPropertyName("profile_path")]
        public string? ProfilePath { get; set; }
    }

    private class TmdbPersonCombinedCreditsResponse
    {
        public List<TmdbPersonCreditCastItem>? Cast { get; set; }
    }

    private class TmdbPersonCreditCastItem
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Name { get; set; }
        [JsonPropertyName("media_type")]
        public string? MediaType { get; set; }
        public string? Character { get; set; }
        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }
        [JsonPropertyName("backdrop_path")]
        public string? BackdropPath { get; set; }
        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }
        [JsonPropertyName("first_air_date")]
        public string? FirstAirDate { get; set; }
        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; set; }
        public double Popularity { get; set; }
        public string? Overview { get; set; }
        [JsonPropertyName("genre_ids")]
        public List<int>? GenreIds { get; set; }
    }
    #endregion
}
