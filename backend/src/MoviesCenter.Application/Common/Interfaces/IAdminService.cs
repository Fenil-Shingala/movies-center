using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.Admin.Dtos;

namespace MoviesCenter.Application.Common.Interfaces;

public interface IAdminService
{
    Task<AdminStatsOverviewDto> GetStatsOverviewAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AdminTitleListItemDto>> GetTitlesAsync(string? q = null, string? type = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<bool> ToggleTitleFeaturedAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ToggleTitleActiveAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResult<AdminSourceDto>> GetSourcesAsync(int? titleId = null, string? status = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<AdminSourceDto> CreateSourceAsync(CreateAdminSourceRequest request, CancellationToken cancellationToken = default);
    Task<AdminSourceDto> UpdateSourceAsync(int id, UpdateAdminSourceRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteSourceAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResult<AdminReportDto>> GetReportsAsync(string? status = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<bool> UpdateReportStatusAsync(int id, string status, CancellationToken cancellationToken = default);

    Task<UploadUrlResponseDto> CreateDirectUploadUrlAsync(string fileName, string title, CancellationToken cancellationToken = default);
    Task<UploadStatusDto> GetUploadStatusAsync(string assetId, CancellationToken cancellationToken = default);
}
