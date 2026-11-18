namespace EventPass.Api.Rules;

public static class EventRatingRule
{
    public const int MinRating = 1;
    public const int MaxRating = 5;
    public const int MaxCommentLength = 300;

    public static (bool IsValid, string NormalizedComment, string? ErrorCode) Validate(int rating, string? rawComment)
    {
        if (rating < MinRating || rating > MaxRating)
            return (false, string.Empty, "invalid_rating");

        var comment = rawComment?.Trim() ?? string.Empty;
        if (comment.Length > MaxCommentLength)
            return (false, comment, "invalid_review_comment");

        return (true, comment, null);
    }

    public static (double NewAverage, int NewCount) CalculateNewRating(double currentAverage, int currentCount, int newRating)
    {
        if (currentCount <= 0)
            return (Math.Round((double)newRating, 2), 1);

        var nextCount = currentCount + 1;
        var nextAvg = ((currentAverage * currentCount) + newRating) / nextCount;
        return (Math.Round(nextAvg, 2), nextCount);
    }
}