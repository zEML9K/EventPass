using EventPass.Api.DTO;
using EventPass.Api.Entity;
using EventPass.Api.Repositories;
using EventPass.Api.Rules;

namespace EventPass.Api.Services;

/// <summary>
/// Фасад каталогу подій і локацій: координує звернення до IEventRepository
/// та правила валідації створення події.
/// </summary>
public sealed class EventCatalogFacade
{
    private readonly IEventRepository _eventRepository;

    public EventCatalogFacade(IEventRepository eventRepository) => _eventRepository = eventRepository;

    public Task<List<VenueDto>> GetVenuesAsync() =>
        _eventRepository.GetVenuesAsync();

    public Task<List<EventDto>> GetEventsAsync(string? category = null) =>
        _eventRepository.GetEventsAsync(category);

    public async Task<(bool Success, string? ErrorCode, EventDto? Result)> GetEventByIdAsync(int id)
    {
        var dto = await _eventRepository.GetEventByIdAsync(id);
        return dto == null ? (false, "event_not_found", null) : (true, null, dto);
    }

    public async Task<(bool Success, string? ErrorCode, EventDto? Result)> CreateEventAsync(CreateEventRequest request)
    {
        var venue = await _eventRepository.GetVenueByIdAsync(request.VenueId);
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

        await _eventRepository.AddEventAsync(ev);

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