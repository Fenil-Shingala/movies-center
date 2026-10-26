using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Ratings.Dtos;

namespace MoviesCenter.Application.Features.Ratings.Commands;

public sealed record RemoveRatingCommand(int TitleId, Guid UserId) : IRequest<RatingDto>;

public class RemoveRatingCommandHandler : IRequestHandler<RemoveRatingCommand, RatingDto>
{
    private readonly IRatingService _ratingService;

    public RemoveRatingCommandHandler(IRatingService ratingService)
    {
        _ratingService = ratingService;
    }

    public async Task<RatingDto> Handle(RemoveRatingCommand request, CancellationToken cancellationToken)
    {
        return await _ratingService.RemoveRatingAsync(request.TitleId, request.UserId, cancellationToken);
    }
}
