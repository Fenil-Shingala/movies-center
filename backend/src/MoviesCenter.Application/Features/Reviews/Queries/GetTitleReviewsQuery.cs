using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Reviews.Dtos;

namespace MoviesCenter.Application.Features.Reviews.Queries;

public sealed record GetTitleReviewsQuery(
    int TitleId,
    int Page = 1,
    int PageSize = 10,
    string Sort = "popular",
    Guid? CurrentUserId = null
) : IRequest<ReviewsListResponseDto>;

public class GetTitleReviewsQueryHandler : IRequestHandler<GetTitleReviewsQuery, ReviewsListResponseDto>
{
    private readonly IReviewService _reviewService;

    public GetTitleReviewsQueryHandler(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<ReviewsListResponseDto> Handle(GetTitleReviewsQuery request, CancellationToken cancellationToken)
    {
        return await _reviewService.GetTitleReviewsAsync(
            request.TitleId,
            request.Page,
            request.PageSize,
            request.Sort,
            request.CurrentUserId,
            cancellationToken
        );
    }
}
