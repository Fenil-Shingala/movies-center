using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Ratings.Dtos;

namespace MoviesCenter.Application.Features.Ratings.Commands;

public sealed record SubmitRatingCommand(int TitleId, Guid UserId, int Score) : IRequest<RatingDto>;

public class SubmitRatingCommandHandler : IRequestHandler<SubmitRatingCommand, RatingDto>
{
    private readonly IRatingService _ratingService;

    public SubmitRatingCommandHandler(IRatingService ratingService)
    {
        _ratingService = ratingService;
    }

    public async Task<RatingDto> Handle(SubmitRatingCommand request, CancellationToken cancellationToken)
    {
        return await _ratingService.SubmitRatingAsync(request.TitleId, request.UserId, request.Score, cancellationToken);
    }
}
