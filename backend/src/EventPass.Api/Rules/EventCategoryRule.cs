namespace EventPass.Api.Rules;

public static class EventCategoryRule
{
    public static (bool IsValid, string NormalizedCategory, string? ErrorCode) ValidateAndNormalize(string? category)
    {
        var trimmed = (category ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return (false, string.Empty, "invalid_category");
        }

        if (trimmed.Length > 50)
        {
            return (false, string.Empty, "category_too_long");
        }

        return (true, trimmed, null);
    }
}