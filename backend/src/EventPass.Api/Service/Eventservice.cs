using EventPass.Api.DTO;
using EventPass.Api.Entity;
using EventPass.Api.Repositories;
using EventPass.Api.Rules;

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
        var venue = await _repo.GetVenueByIdAsync(request.VenueId);
        var validation = EventCreationValidator.Validate(request, venue, DateTime.UtcNow);

        if (!validation.IsValid || venue == null)
            return (false, validation.ErrorCode, null);

        var ev = new Event
        {
            Title = validation.Title,
            Description = validation.Description,
            Date = validation.UtcDate,
            TotalTickets = request.TotalTickets,
            AvailableSeats = request.TotalTickets,
            VenueId = venue.Id,
            Category = validation.Category,
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
}