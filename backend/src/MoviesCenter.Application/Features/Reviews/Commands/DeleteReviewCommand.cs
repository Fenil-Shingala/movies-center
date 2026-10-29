using MediatR;
using MoviesCenter.Application.Common.Interfaces;

namespace MoviesCenter.Application.Features.Reviews.Commands;

public sealed record DeleteReviewCommand(
    int ReviewId,
    Guid UserId,
    bool IsAdmin
) : IRequest<bool>;

public class DeleteReviewCommandHandler : IRequestHandler<DeleteReviewCommand, bool>
{
    private readonly IReviewService _reviewService;

    public DeleteReviewCommandHandler(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<bool> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        return await _reviewService.DeleteReviewAsync(
            request.ReviewId,
            request.UserId,
            request.IsAdmin,
            cancellationToken
        );
    }
}
