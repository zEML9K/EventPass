using EventPass.Api.DTO;
using EventPass.Api.Entity;

namespace EventPass.Api.Repositories;

public interface IEventRepository
{
    // Події та локації
    Task<List<VenueDto>> GetVenuesAsync();
    Task<Venue?> GetVenueByIdAsync(int venueId);
    Task<List<EventDto>> GetEventsAsync();
    Task<EventDto?> GetEventByIdAsync(int id);
    Task<Event> AddEventAsync(Event ev);
}