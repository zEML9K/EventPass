using EventPass.Api.DTO;
using EventPass.Api.Entity;
using EventPass.Api.Repositories;
using EventPass.Api.Rules;

namespace EventPass.Api.Services;

public sealed class EventService
{
    private readonly IEventRepository _repo;

    public EventService(IEventRepository repo) => _repo = repo;

    // Список локацій (для вибору при створенні події)
    public Task<List<VenueDto>> GetVenuesAsync() => _repo.GetVenuesAsync();

    // Category event and filter category
    public Task<List<EventDto>> GetAllAsync(string? category = null) => _repo.GetEventsAsync(category);

    // UC-01: одна подія
    public async Task<(bool Success, string? ErrorCode, EventDto? Result)> GetByIdAsync(int id)
    {
        var dto = await _repo.GetEventByIdAsync(id);
        return dto == null ? (false, "event_not_found", null) : (true, null, dto);
    }

    // UC-02: створити подію
    public async Task<(bool Success, string? ErrorCode, EventDto? Result)> CreateEventAsync(CreateEventRequest request)
    {
        // A1: коректність даних
        var title = request.Title?.Trim();
        if (string.IsNullOrWhiteSpace(title) || title.Length > 200)
            return (false, "invalid_title", null);

        var description = request.Description?.Trim();
        if (string.IsNullOrWhiteSpace(description))
            return (false, "invalid_description", null);

        // Category event and filter category (предметне правило)
        var (isCategoryValid, category, categoryError) = EventCategoryRule.ValidateAndNormalize(request.Category);
        if (!isCategoryValid)
            return (false, categoryError, null);

        var date = request.Date.Kind == DateTimeKind.Local
            ? request.Date.ToUniversalTime()
            : DateTime.SpecifyKind(request.Date, DateTimeKind.Utc);
        if (date <= DateTime.UtcNow)
            return (false, "invalid_date", null);

        if (request.TotalTickets <= 0)
            return (false, "invalid_total_tickets", null);

        // Локація має існувати
        var venue = await _repo.GetVenueByIdAsync(request.VenueId);
        if (venue == null)
            return (false, "venue_not_found", null);

        // A2: квитків не більше, ніж місткість
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
            Category = category,
            AverageRating = 0,
            ReviewsCount = 0
        };

        await _repo.AddEventAsync(ev);

        return (true, null, new EventDto(
            ev.Id,
            ev.Title,
            ev.Description,
            ev.Date,
            ev.TotalTickets,
            ev.AvailableSeats,
            venue.Name,
            venue.Address,
            ev.Category,
            ev.AverageRating,
            ev.ReviewsCount));
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

        var (newAvg, newCount) = EventRatingRule.CalculateNewRating(
            existing.AverageRating, existing.ReviewsCount, request.Rating);

        var clientId = (request.ClientId ?? string.Empty).Trim();
        if (clientId.Length > 64) clientId = clientId[..64];

        var ip = (clientIp ?? string.Empty).Trim();
        if (ip.Length > 64) ip = ip[..64];

        return await _repo.AddReviewAsync(eventId, request.Rating, comment, newAvg, newCount, clientId, ip);
    }

    // Скарги / репорти на подію
    public async Task<(bool Success, string? ErrorCode)> ReportEventAsync(int eventId, CreateReportRequest request, string? clientIp = null)
    {
        if (string.IsNullOrWhiteSpace(request.Category))
        {
            return (false, "invalid_report_category");
        }

        var comment = request.Comment ?? string.Empty;
        if (comment.Length > 500)
        {
            return (false, "invalid_report_comment");
        }

        var normalizedClientId = (request.ClientId ?? string.Empty).Trim();
        if (normalizedClientId.Length > 64)
        {
            normalizedClientId = normalizedClientId[..64];
        }

        var normalizedIp = (clientIp ?? string.Empty).Trim();
        if (normalizedIp.Length > 64)
        {
            normalizedIp = normalizedIp[..64];
        }

        return await _repo.AddReportAsync(eventId, request.Category.Trim(), comment, normalizedClientId, normalizedIp);
    }
}