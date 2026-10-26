using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.UserData.Dtos;

namespace MoviesCenter.Application.Common.Interfaces;

public interface IUserDataService
{
    Task<PagedResult<WatchlistItemDto>> GetWatchlistAsync(Guid userId, string? type = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<bool> AddToWatchlistAsync(Guid userId, int titleId, CancellationToken cancellationToken = default);
    Task<bool> RemoveFromWatchlistAsync(Guid userId, int titleId, CancellationToken cancellationToken = default);
    Task<bool> IsInWatchlistAsync(Guid userId, int titleId, CancellationToken cancellationToken = default);

    Task<PagedResult<WatchHistoryItemDto>> GetWatchHistoryAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<WatchHistoryItemDto> UpdateWatchProgressAsync(Guid userId, UpdateWatchProgressRequest request, CancellationToken cancellationToken = default);
    Task<bool> RemoveFromWatchHistoryAsync(Guid userId, int historyId, CancellationToken cancellationToken = default);
    Task<bool> ClearWatchHistoryAsync(Guid userId, CancellationToken cancellationToken = default);
}
