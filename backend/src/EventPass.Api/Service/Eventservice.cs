using EventPass.Api.Rules;
using EventPass.Api.DTO;
using EventPass.Api.Entity;
using EventPass.Api.Repositories;

namespace EventPass.Api.Services;

public sealed class EventService
{
    private readonly IEventRepository _repo;

    public EventService(IEventRepository repo) => _repo = repo;

    public Task<List<VenueDto>> GetVenuesAsync() => _repo.GetVenuesAsync();

    public Task<List<EventDto>> GetAllAsync(string? category = null) => _repo.GetEventsAsync(category);

    public async Task<(bool Success, string? ErrorCode, EventDto? Result)> GetByIdAsync(int id)
    {
        var dto = await _repo.GetEventByIdAsync(id);
        return dto == null ? (false, "event_not_found", null) : (true, null, dto);
    }

    public async Task<(bool Success, string? ErrorCode, EventDto? Result)> CreateEventAsync(CreateEventRequest request)
    {
        var title = request.Title?.Trim();
        if (string.IsNullOrWhiteSpace(title) || title.Length > 200)
            return (false, "invalid_title", null);

        var description = request.Description?.Trim();
        if (string.IsNullOrWhiteSpace(description))
            return (false, "invalid_description", null);

        var category = request.Category?.Trim() ?? string.Empty;
        if (category.Length > 50)
            return (false, "invalid_category", null);

        var date = request.Date.Kind == DateTimeKind.Local
            ? request.Date.ToUniversalTime()
            : DateTime.SpecifyKind(request.Date, DateTimeKind.Utc);
        if (date <= DateTime.UtcNow)
            return (false, "invalid_date", null);

        if (request.TotalTickets <= 0)
            return (false, "invalid_total_tickets", null);

        var venue = await _repo.GetVenueByIdAsync(request.VenueId);
        if (venue == null)
            return (false, "venue_not_found", null);

        if (request.TotalTickets > venue.Capacity)
            return (false, "tickets_exceed_capacity", null);

        var ev = new Event
        {
            Title = title,
            Description = description,
            Date = date,
            TotalTickets = request.TotalTickets,
            AvailableSeats = request.TotalTickets,
            VenueId = venue.Id,
            Category = category
        };

        await _repo.AddEventAsync(ev);

        return (true, null, new EventDto(ev.Id, ev.Title, ev.Description, ev.Date,
            ev.TotalTickets, ev.AvailableSeats, venue.Name, venue.Address,
            ev.Category));
    }

    // Оцінки та відгуки про подію
    public Task<List<EventReviewDto>> GetReviewsAsync(int eventId) => _repo.GetReviewsAsync(eventId);

    public async Task<(bool Success, string? ErrorCode, EventDto? Result)> RateEventAsync(int eventId, RateEventRequest request, string? clientIp = null)
    {
        var (isValid, comment, errorCode) = EventRatingRule.Validate(request.Rating, request.Comment);
        if (!isValid)
            return (false, errorCode, null);

        var existing = await _repo.GetEventByIdAsync(eventId);
        if (existing == null)
            return (false, "event_not_found", null);

        var normalizedClientId = (request.ClientId ?? string.Empty).Trim();
        if (normalizedClientId.Length > 64)
            normalizedClientId = normalizedClientId[..64];

        var normalizedIp = (clientIp ?? string.Empty).Trim();
        if (normalizedIp.Length > 64)
            normalizedIp = normalizedIp[..64];

        var (newAvg, newCount) = EventRatingRule.CalculateNewRating(
            existing.AverageRating, existing.ReviewsCount, request.Rating);

        return await _repo.AddReviewAsync(eventId, request.Rating, comment, newAvg, newCount, normalizedClientId, normalizedIp);
    }
}

