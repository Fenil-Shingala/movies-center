using MoviesCenter.Application.Features.Admin.Dtos;

namespace MoviesCenter.Application.Common.Interfaces;

public interface ITmdbSyncService
{
    Task<TmdbSyncResultDto> RunFullSyncAsync(string triggeredBy = "Admin", int? limit = null, CancellationToken cancellationToken = default);
    Task<TmdbSyncStatusDto> GetSyncStatusAsync(CancellationToken cancellationToken = default);
    Task<bool> SyncSingleTitleAsync(int tmdbId, string type, CancellationToken cancellationToken = default);
}
