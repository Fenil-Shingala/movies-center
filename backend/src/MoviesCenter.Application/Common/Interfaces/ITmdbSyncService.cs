using MoviesCenter.Application.Features.Admin.Dtos;

namespace MoviesCenter.Application.Common.Interfaces;

public interface ITmdbSyncService
{
    Task<TmdbSyncResultDto> RunFullSyncAsync(string triggeredBy = "Admin", CancellationToken cancellationToken = default);
    Task<TmdbSyncStatusDto> GetSyncStatusAsync(CancellationToken cancellationToken = default);
    Task<bool> SyncSingleTitleAsync(int tmdbId, string type, CancellationToken cancellationToken = default);
}
