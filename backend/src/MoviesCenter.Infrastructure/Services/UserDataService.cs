using Microsoft.EntityFrameworkCore;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.UserData.Dtos;
using MoviesCenter.Domain.Entities;
using MoviesCenter.Infrastructure.Data;

namespace MoviesCenter.Infrastructure.Services;

public class UserDataService : IUserDataService
{
    private readonly ApplicationDbContext _dbContext;

    public UserDataService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<WatchlistItemDto>> GetWatchlistAsync(
        Guid userId,
        string? type = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.WatchlistItems
            .Include(w => w.Title)
            .Where(w => w.UserId == userId);

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(w => w.Title.Type == type);
        }

        var total = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling((double)total / pageSize);

        var list = await query
            .OrderByDescending(w => w.AddedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = list.Select(w =>
        {
            var date = w.Title.ReleaseDate ?? w.Title.FirstAirDate;
            var year = date?.Year.ToString();
            var rating = (double)(w.Title.TmdbRating ?? w.Title.ImdbRating ?? 0);

            return new WatchlistItemDto(
                w.Id,
                w.TitleId,
                w.Title.TitleText,
                w.Title.PosterPath,
                w.Title.BackdropPath,
                w.Title.Type,
                date?.ToString("yyyy-MM-dd"),
                year,
                rating,
                w.AddedAt
            );
        }).ToList();

        return new PagedResult<WatchlistItemDto>(items, page, pageSize, total, totalPages);
    }

    public async Task<bool> AddToWatchlistAsync(
        Guid userId,
        int titleId,
        CancellationToken cancellationToken = default)
    {
        var titleExists = await _dbContext.Titles.AnyAsync(t => t.Id == titleId, cancellationToken);
        if (!titleExists)
        {
            throw new KeyNotFoundException($"Title with ID {titleId} does not exist.");
        }

        var existing = await _dbContext.WatchlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.TitleId == titleId, cancellationToken);

        if (existing != null)
        {
            return true; // Already in watchlist
        }

        _dbContext.WatchlistItems.Add(new WatchlistItem
        {
            UserId = userId,
            TitleId = titleId,
            AddedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveFromWatchlistAsync(
        Guid userId,
        int titleId,
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.WatchlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.TitleId == titleId, cancellationToken);

        if (item == null)
        {
            return false;
        }

        _dbContext.WatchlistItems.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> IsInWatchlistAsync(
        Guid userId,
        int titleId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.WatchlistItems
            .AnyAsync(w => w.UserId == userId && w.TitleId == titleId, cancellationToken);
    }

    public async Task<PagedResult<WatchHistoryItemDto>> GetWatchHistoryAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.WatchHistoryItems
            .Include(h => h.Title)
            .Include(h => h.Episode)
            .Where(h => h.UserId == userId);

        var total = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling((double)total / pageSize);

        var list = await query
            .OrderByDescending(h => h.WatchedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = list.Select(h =>
        {
            var percent = (h.DurationSeconds.HasValue && h.DurationSeconds.Value > 0)
                ? Math.Min(100, (int)Math.Round((double)h.ProgressSeconds / h.DurationSeconds.Value * 100))
                : 0;

            return new WatchHistoryItemDto(
                h.Id,
                h.TitleId,
                h.Title.TitleText,
                h.Title.PosterPath,
                h.Title.BackdropPath,
                h.Title.Type,
                h.EpisodeId,
                h.SeasonNumber,
                h.EpisodeNumber,
                h.Episode?.Name,
                h.ProgressSeconds,
                h.DurationSeconds,
                percent,
                h.IsCompleted,
                h.WatchedAt
            );
        }).ToList();

        return new PagedResult<WatchHistoryItemDto>(items, page, pageSize, total, totalPages);
    }

    public async Task<WatchHistoryItemDto> UpdateWatchProgressAsync(
        Guid userId,
        UpdateWatchProgressRequest request,
        CancellationToken cancellationToken = default)
    {
        var title = await _dbContext.Titles.FirstOrDefaultAsync(t => t.Id == request.TitleId, cancellationToken);
        if (title == null)
        {
            throw new KeyNotFoundException($"Title with ID {request.TitleId} does not exist.");
        }

        var match = await _dbContext.WatchHistoryItems
            .Include(h => h.Episode)
            .FirstOrDefaultAsync(h =>
                h.UserId == userId &&
                h.TitleId == request.TitleId &&
                h.EpisodeId == request.EpisodeId,
                cancellationToken);

        var isCompleted = request.DurationSeconds.HasValue && request.DurationSeconds.Value > 0 &&
            request.ProgressSeconds >= (int)(request.DurationSeconds.Value * 0.9);

        if (match != null)
        {
            match.ProgressSeconds = request.ProgressSeconds;
            if (request.DurationSeconds.HasValue && request.DurationSeconds.Value > 0)
            {
                match.DurationSeconds = request.DurationSeconds.Value;
            }
            if (request.SeasonNumber.HasValue) match.SeasonNumber = request.SeasonNumber.Value;
            if (request.EpisodeNumber.HasValue) match.EpisodeNumber = request.EpisodeNumber.Value;
            match.WatchedAt = DateTime.UtcNow;
            if (isCompleted) match.IsCompleted = true;

            await _dbContext.SaveChangesAsync(cancellationToken);

            var percent = (match.DurationSeconds.HasValue && match.DurationSeconds.Value > 0)
                ? Math.Min(100, (int)Math.Round((double)match.ProgressSeconds / match.DurationSeconds.Value * 100))
                : 0;

            return new WatchHistoryItemDto(
                match.Id,
                match.TitleId,
                title.TitleText,
                title.PosterPath,
                title.BackdropPath,
                title.Type,
                match.EpisodeId,
                match.SeasonNumber,
                match.EpisodeNumber,
                match.Episode?.Name,
                match.ProgressSeconds,
                match.DurationSeconds,
                percent,
                match.IsCompleted,
                match.WatchedAt
            );
        }
        else
        {
            var newItem = new WatchHistoryItem
            {
                UserId = userId,
                TitleId = request.TitleId,
                EpisodeId = request.EpisodeId,
                SeasonNumber = request.SeasonNumber,
                EpisodeNumber = request.EpisodeNumber,
                ProgressSeconds = request.ProgressSeconds,
                DurationSeconds = request.DurationSeconds,
                IsCompleted = isCompleted,
                WatchedAt = DateTime.UtcNow
            };

            _dbContext.WatchHistoryItems.Add(newItem);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var percent = (newItem.DurationSeconds.HasValue && newItem.DurationSeconds.Value > 0)
                ? Math.Min(100, (int)Math.Round((double)newItem.ProgressSeconds / newItem.DurationSeconds.Value * 100))
                : 0;

            return new WatchHistoryItemDto(
                newItem.Id,
                newItem.TitleId,
                title.TitleText,
                title.PosterPath,
                title.BackdropPath,
                title.Type,
                newItem.EpisodeId,
                newItem.SeasonNumber,
                newItem.EpisodeNumber,
                null,
                newItem.ProgressSeconds,
                newItem.DurationSeconds,
                percent,
                newItem.IsCompleted,
                newItem.WatchedAt
            );
        }
    }

    public async Task<bool> RemoveFromWatchHistoryAsync(
        Guid userId,
        int historyId,
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.WatchHistoryItems
            .FirstOrDefaultAsync(h => h.Id == historyId && h.UserId == userId, cancellationToken);

        if (item == null)
        {
            return false;
        }

        _dbContext.WatchHistoryItems.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ClearWatchHistoryAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.WatchHistoryItems
            .Where(h => h.UserId == userId)
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return true;
        }

        _dbContext.WatchHistoryItems.RemoveRange(items);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
