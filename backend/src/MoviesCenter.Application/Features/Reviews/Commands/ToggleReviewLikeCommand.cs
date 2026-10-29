using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Reviews.Dtos;

namespace MoviesCenter.Application.Features.Reviews.Commands;

public sealed record ToggleReviewLikeCommand(
    int ReviewId,
    Guid UserId
) : IRequest<ToggleLikeResponseDto>;

public class ToggleReviewLikeCommandHandler : IRequestHandler<ToggleReviewLikeCommand, ToggleLikeResponseDto>
{
    private readonly IReviewService _reviewService;

    public ToggleReviewLikeCommandHandler(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<ToggleLikeResponseDto> Handle(ToggleReviewLikeCommand request, CancellationToken cancellationToken)
    {
        return await _reviewService.ToggleReviewLikeAsync(
            request.ReviewId,
            request.UserId,
            cancellationToken
        );
    }
}
