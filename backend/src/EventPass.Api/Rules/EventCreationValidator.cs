using EventPass.Api.DTO;
using EventPass.Api.Entity;

namespace EventPass.Api.Rules;

public static class EventCreationValidator
{
    public static (bool IsValid, string? ErrorCode, string Title, string Description, string Category, DateTime UtcDate) Validate(
        CreateEventRequest request,
        Venue? venue,
        DateTime utcNow)
    {
        var title = request.Title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title) || title.Length > 200)
            return (false, "invalid_title", string.Empty, string.Empty, string.Empty, default);

        var description = request.Description?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(description))
            return (false, "invalid_description", string.Empty, string.Empty, string.Empty, default);

        var (isCategoryValid, category, categoryError) = EventCategoryRule.ValidateAndNormalize(request.Category);
        if (!isCategoryValid)
            return (false, categoryError, string.Empty, string.Empty, string.Empty, default);

        var utcDate = request.Date.Kind == DateTimeKind.Local
            ? request.Date.ToUniversalTime()
            : DateTime.SpecifyKind(request.Date, DateTimeKind.Utc);

        if (utcDate <= utcNow)
            return (false, "invalid_date", string.Empty, string.Empty, string.Empty, default);

        if (request.TotalTickets <= 0)
            return (false, "invalid_total_tickets", string.Empty, string.Empty, string.Empty, default);

        if (venue == null)
            return (false, "venue_not_found", string.Empty, string.Empty, string.Empty, default);

        if (request.TotalTickets > venue.Capacity)
            return (false, "tickets_exceed_capacity", string.Empty, string.Empty, string.Empty, default);

        return (true, null, title, description, category, utcDate);
    }
}