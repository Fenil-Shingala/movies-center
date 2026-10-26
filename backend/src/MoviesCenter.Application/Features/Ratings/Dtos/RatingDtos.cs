namespace MoviesCenter.Application.Features.Ratings.Dtos;

public sealed record RatingDto(
    double AverageRating,
    int TotalRatings,
    int? UserRating,
    Dictionary<int, int> Distribution
);

public sealed record SubmitRatingRequest(int Score);
