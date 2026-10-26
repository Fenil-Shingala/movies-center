using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Ratings.Dtos;

namespace MoviesCenter.Application.Features.Ratings.Queries;

public sealed record GetTitleRatingQuery(int TitleId, Guid? UserId) : IRequest<RatingDto>;

public class GetTitleRatingQueryHandler : IRequestHandler<GetTitleRatingQuery, RatingDto>
{
    private readonly IRatingService _ratingService;

    public GetTitleRatingQueryHandler(IRatingService ratingService)
    {
        _ratingService = ratingService;
    }

    public async Task<RatingDto> Handle(GetTitleRatingQuery request, CancellationToken cancellationToken)
    {
        return await _ratingService.GetTitleRatingAsync(request.TitleId, request.UserId, cancellationToken);
    }
}
