using MoviesCenter.Application.Features.Ratings.Dtos;

namespace MoviesCenter.Application.Common.Interfaces;

public interface IRatingService
{
    Task<RatingDto> GetTitleRatingAsync(int titleId, Guid? userId, CancellationToken cancellationToken = default);
    Task<RatingDto> SubmitRatingAsync(int titleId, Guid userId, int score, CancellationToken cancellationToken = default);
    Task<RatingDto> RemoveRatingAsync(int titleId, Guid userId, CancellationToken cancellationToken = default);
}
