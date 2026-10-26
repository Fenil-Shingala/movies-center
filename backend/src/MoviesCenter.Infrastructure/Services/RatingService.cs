using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Ratings.Dtos;
using MoviesCenter.Domain.Entities;
using MoviesCenter.Infrastructure.Data;

namespace MoviesCenter.Infrastructure.Services;

public class RatingService : IRatingService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RatingService> _logger;

    public RatingService(ApplicationDbContext dbContext, ILogger<RatingService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<RatingDto> GetTitleRatingAsync(int titleId, Guid? userId, CancellationToken cancellationToken = default)
    {
        var ratings = await _dbContext.Ratings
            .AsNoTracking()
            .Where(r => r.TitleId == titleId)
            .Select(r => new { r.UserId, r.Score })
            .ToListAsync(cancellationToken);

        int totalRatings = ratings.Count;
        double averageRating = totalRatings > 0 ? Math.Round(ratings.Average(r => r.Score), 1) : 0.0;

        var distribution = new Dictionary<int, int>
        {
            { 1, ratings.Count(r => r.Score == 1) },
            { 2, ratings.Count(r => r.Score == 2) },
            { 3, ratings.Count(r => r.Score == 3) },
            { 4, ratings.Count(r => r.Score == 4) },
            { 5, ratings.Count(r => r.Score == 5) }
        };

        int? userRating = userId.HasValue
            ? ratings.FirstOrDefault(r => r.UserId == userId.Value)?.Score
            : null;

        return new RatingDto(averageRating, totalRatings, userRating, distribution);
    }

    public async Task<RatingDto> SubmitRatingAsync(int titleId, Guid userId, int score, CancellationToken cancellationToken = default)
    {
        if (score < 1 || score > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(score), "Rating score must be between 1 and 5.");
        }

        var titleExists = await _dbContext.Titles.AnyAsync(t => t.Id == titleId, cancellationToken);
        if (!titleExists)
        {
            throw new KeyNotFoundException($"Title with ID {titleId} not found.");
        }

        var existing = await _dbContext.Ratings
            .FirstOrDefaultAsync(r => r.UserId == userId && r.TitleId == titleId, cancellationToken);

        if (existing != null)
        {
            existing.Score = score;
            existing.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("Updated rating for user {UserId} on title {TitleId} to {Score}", userId, titleId, score);
        }
        else
        {
            _dbContext.Ratings.Add(new Rating
            {
                UserId = userId,
                TitleId = titleId,
                Score = score,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            _logger.LogInformation("Added new rating for user {UserId} on title {TitleId} with score {Score}", userId, titleId, score);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetTitleRatingAsync(titleId, userId, cancellationToken);
    }

    public async Task<RatingDto> RemoveRatingAsync(int titleId, Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Ratings
            .FirstOrDefaultAsync(r => r.UserId == userId && r.TitleId == titleId, cancellationToken);

        if (existing != null)
        {
            _dbContext.Ratings.Remove(existing);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Removed rating for user {UserId} on title {TitleId}", userId, titleId);
        }

        return await GetTitleRatingAsync(titleId, userId, cancellationToken);
    }
}
