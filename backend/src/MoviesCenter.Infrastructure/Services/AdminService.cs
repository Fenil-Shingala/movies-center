using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.Admin.Dtos;
using MoviesCenter.Domain.Entities;
using MoviesCenter.Infrastructure.Data;

namespace MoviesCenter.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly ITmdbSyncService _syncService;
    private readonly IConfiguration _config;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        ApplicationDbContext db,
        ITmdbSyncService syncService,
        IConfiguration config,
        ILogger<AdminService> logger)
    {
        _db = db;
        _syncService = syncService;
        _config = config;
        _logger = logger;
    }

    public async Task<AdminStatsOverviewDto> GetStatsOverviewAsync(CancellationToken cancellationToken = default)
    {
        var totalTitles = await _db.Titles.CountAsync(cancellationToken);
        var totalMovies = await _db.Titles.CountAsync(t => t.Type == "movie", cancellationToken);
        var totalSeries = await _db.Titles.CountAsync(t => t.Type == "series", cancellationToken);
        var totalSources = await _db.TitleSources.CountAsync(cancellationToken);
        var pendingReports = await _db.SourceReports.CountAsync(r => r.Status == "Pending", cancellationToken);
        var totalUsers = await _db.Users.CountAsync(cancellationToken);
        var lastSyncStatus = await _syncService.GetSyncStatusAsync(cancellationToken);

        return new AdminStatsOverviewDto(
            TotalTitles: totalTitles,
            TotalMovies: totalMovies,
            TotalSeries: totalSeries,
            TotalSources: totalSources,
            PendingReports: pendingReports,
            TotalUsers: totalUsers,
            LastSyncStatus: lastSyncStatus
        );
    }

    public async Task<PagedResult<AdminTitleListItemDto>> GetTitlesAsync(string? q = null, string? type = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = _db.Titles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var searchTerm = q.Trim();
            query = query.Where(t => t.TitleText.Contains(searchTerm) || (t.OriginalTitle != null && t.OriginalTitle.Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(type) && !type.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(t => t.Type == type.ToLowerInvariant());
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (page < 1) page = 1;

        var items = await query
            .OrderByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new AdminTitleListItemDto(
                t.Id,
                t.TmdbId,
                t.TitleText,
                t.Type,
                t.PosterPath,
                t.ReleaseDate.HasValue ? t.ReleaseDate.Value.Year.ToString() : (t.FirstAirDate.HasValue ? t.FirstAirDate.Value.Year.ToString() : null),
                (double)(t.TmdbRating ?? 0m),
                t.TitleSources.Count,
                t.IsFeatured,
                t.IsActive,
                t.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminTitleListItemDto>(
            Items: items,
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount,
            TotalPages: totalPages
        );
    }

    public async Task<bool> ToggleTitleFeaturedAsync(int id, CancellationToken cancellationToken = default)
    {
        var title = await _db.Titles.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (title == null) return false;

        title.IsFeatured = !title.IsFeatured;
        title.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ToggleTitleActiveAsync(int id, CancellationToken cancellationToken = default)
    {
        var title = await _db.Titles.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (title == null) return false;

        title.IsActive = !title.IsActive;
        title.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PagedResult<AdminSourceDto>> GetSourcesAsync(int? titleId = null, string? status = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = _db.TitleSources
            .Include(ts => ts.Title)
            .Include(ts => ts.SourceProvider)
            .AsNoTracking()
            .AsQueryable();

        if (titleId.HasValue && titleId > 0)
        {
            query = query.Where(ts => ts.TitleId == titleId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("active", StringComparison.OrdinalIgnoreCase))
                query = query.Where(ts => ts.IsActive);
            else if (status.Equals("inactive", StringComparison.OrdinalIgnoreCase))
                query = query.Where(ts => !ts.IsActive);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (page < 1) page = 1;

        var items = await query
            .OrderBy(ts => ts.TitleId)
            .ThenBy(ts => ts.Priority)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ts => new AdminSourceDto(
                ts.Id,
                ts.TitleId,
                ts.Title.TitleText,
                ts.SeasonNumber,
                ts.EpisodeNumber,
                (ts.SeasonNumber.HasValue && ts.EpisodeNumber.HasValue) ? $"S{ts.SeasonNumber.Value:D2}E{ts.EpisodeNumber.Value:D2}" : null,
                ts.SourceProvider != null ? ts.SourceProvider.Name : "Embed",
                ts.EmbedUrl != null ? "embed" : "stream",
                ts.EmbedUrl ?? ts.StreamUrl ?? string.Empty,
                ts.Quality,
                ts.Priority,
                ts.IsActive,
                ts.IsAutoGenerated,
                ts.LastVerifiedAt,
                ts.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminSourceDto>(
            Items: items,
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount,
            TotalPages: totalPages
        );
    }

    public async Task<AdminSourceDto> CreateSourceAsync(CreateAdminSourceRequest request, CancellationToken cancellationToken = default)
    {
        var title = await _db.Titles.FirstOrDefaultAsync(t => t.Id == request.TitleId, cancellationToken);
        if (title == null)
        {
            throw new KeyNotFoundException($"Title with ID {request.TitleId} does not exist.");
        }

        var provider = await _db.SourceProviders
            .FirstOrDefaultAsync(p => p.Name.ToLower() == request.ProviderName.ToLower(), cancellationToken);

        if (provider == null)
        {
            provider = new SourceProvider
            {
                Name = request.ProviderName,
                Slug = request.ProviderName.ToLower().Replace(" ", "-"),
                Type = request.SourceType == "embed" ? "ThirdPartyEmbed" : "Upload",
                Priority = request.Priority,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.SourceProviders.Add(provider);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var source = new TitleSource
        {
            TitleId = request.TitleId,
            SourceProviderId = provider.Id,
            SeasonNumber = request.SeasonNumber,
            EpisodeNumber = request.EpisodeNumber,
            EmbedUrl = request.SourceType == "embed" ? request.Url : null,
            StreamUrl = request.SourceType != "embed" ? request.Url : null,
            Quality = request.Quality ?? "1080p",
            Priority = request.Priority > 0 ? request.Priority : 1,
            IsActive = request.IsActive,
            IsAutoGenerated = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.TitleSources.Add(source);
        await _db.SaveChangesAsync(cancellationToken);

        string? epString = (request.SeasonNumber.HasValue && request.EpisodeNumber.HasValue) 
            ? $"S{request.SeasonNumber.Value:D2}E{request.EpisodeNumber.Value:D2}" 
            : null;

        return new AdminSourceDto(
            source.Id,
            source.TitleId,
            title.TitleText,
            source.SeasonNumber,
            source.EpisodeNumber,
            epString,
            provider.Name,
            request.SourceType,
            request.Url,
            source.Quality,
            source.Priority,
            source.IsActive,
            source.IsAutoGenerated,
            source.LastVerifiedAt,
            source.CreatedAt
        );
    }

    public async Task<AdminSourceDto> UpdateSourceAsync(int id, UpdateAdminSourceRequest request, CancellationToken cancellationToken = default)
    {
        var source = await _db.TitleSources
            .Include(ts => ts.Title)
            .Include(ts => ts.SourceProvider)
            .FirstOrDefaultAsync(ts => ts.Id == id, cancellationToken);

        if (source == null)
        {
            throw new KeyNotFoundException($"Source with ID {id} not found.");
        }

        var provider = await _db.SourceProviders
            .FirstOrDefaultAsync(p => p.Name.ToLower() == request.ProviderName.ToLower(), cancellationToken);

        if (provider != null)
        {
            source.SourceProviderId = provider.Id;
        }

        source.SeasonNumber = request.SeasonNumber;
        source.EpisodeNumber = request.EpisodeNumber;
        source.EmbedUrl = request.SourceType == "embed" ? request.Url : null;
        source.StreamUrl = request.SourceType != "embed" ? request.Url : null;
        source.Quality = request.Quality ?? source.Quality;
        source.Priority = request.Priority;
        source.IsActive = request.IsActive;
        source.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        string? epString = (source.SeasonNumber.HasValue && source.EpisodeNumber.HasValue) 
            ? $"S{source.SeasonNumber.Value:D2}E{source.EpisodeNumber.Value:D2}" 
            : null;

        return new AdminSourceDto(
            source.Id,
            source.TitleId,
            source.Title.TitleText,
            source.SeasonNumber,
            source.EpisodeNumber,
            epString,
            provider?.Name ?? source.SourceProvider?.Name ?? request.ProviderName,
            request.SourceType,
            request.Url,
            source.Quality,
            source.Priority,
            source.IsActive,
            source.IsAutoGenerated,
            source.LastVerifiedAt,
            source.CreatedAt
        );
    }

    public async Task<bool> DeleteSourceAsync(int id, CancellationToken cancellationToken = default)
    {
        var source = await _db.TitleSources.FirstOrDefaultAsync(ts => ts.Id == id, cancellationToken);
        if (source == null) return false;

        _db.TitleSources.Remove(source);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PagedResult<AdminReportDto>> GetReportsAsync(string? status = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = _db.SourceReports
            .Include(r => r.Title)
            .Include(r => r.Source)
                .ThenInclude(s => s!.SourceProvider)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(r => r.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (page < 1) page = 1;

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new AdminReportDto(
                r.Id,
                r.SourceId,
                r.TitleId,
                r.Title != null ? r.Title.TitleText : $"Title #{r.TitleId}",
                r.Source != null ? (r.Source.EmbedUrl ?? r.Source.StreamUrl) : null,
                r.Source != null && r.Source.SourceProvider != null ? r.Source.SourceProvider.Name : "Embed",
                r.Reason,
                r.Details,
                r.Season,
                r.Episode,
                r.Status,
                r.CreatedAt,
                r.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminReportDto>(
            Items: items,
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount,
            TotalPages: totalPages
        );
    }

    public async Task<bool> UpdateReportStatusAsync(int id, string status, CancellationToken cancellationToken = default)
    {
        var report = await _db.SourceReports.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (report == null) return false;

        report.Status = status;
        report.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<UploadUrlResponseDto> CreateDirectUploadUrlAsync(string fileName, string title, CancellationToken cancellationToken = default)
    {
        var libraryId = _config["BUNNY_LIBRARY_ID"] ?? "demo-library";
        var apiKey = _config["BUNNY_API_KEY"];
        var assetId = $"video_{Guid.NewGuid():N}";

        if (!string.IsNullOrEmpty(apiKey) && libraryId != "demo-library")
        {
            // Real Bunny Stream TUS upload URL
            var uploadUrl = $"https://video.bunnycdn.com/library/{libraryId}/videos/{assetId}";
            var playUrl = $"https://iframe.mediadelivery.net/embed/{libraryId}/{assetId}";
            return Task.FromResult(new UploadUrlResponseDto(assetId, uploadUrl, playUrl, "Pending", "Direct upload endpoint generated."));
        }

        // Development / simulation mode fallback
        var simUploadUrl = $"/api/v1/admin/uploads/mock/{assetId}";
        var simPlayUrl = $"https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4";
        return Task.FromResult(new UploadUrlResponseDto(assetId, simUploadUrl, simPlayUrl, "ReadyForUpload", "Simulation mode: direct upload simulation enabled."));
    }

    public Task<UploadStatusDto> GetUploadStatusAsync(string assetId, CancellationToken cancellationToken = default)
    {
        // Returns completed/ready status for simulation or query
        return Task.FromResult(new UploadStatusDto(
            AssetId: assetId,
            Status: "Ready",
            ProgressPercent: 100,
            StreamUrl: $"https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"
        ));
    }
}
