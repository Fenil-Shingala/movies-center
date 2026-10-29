using MediatR;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Reviews.Dtos;

namespace MoviesCenter.Application.Features.Reviews.Commands;

public sealed record CreateReviewCommand(
    int TitleId,
    Guid UserId,
    CreateReviewRequest Request
) : IRequest<ReviewDto>;

public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, ReviewDto>
{
    private readonly IReviewService _reviewService;

    public CreateReviewCommandHandler(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<ReviewDto> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        return await _reviewService.CreateReviewAsync(
            request.TitleId,
            request.UserId,
            request.Request,
            cancellationToken
        );
    }
}
