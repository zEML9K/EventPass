using EventPass.Api.DTO;
using EventPass.Api.Entity;

namespace EventPass.Api.Repositories;

public interface IEventRepository
{
    Task<List<VenueDto>> GetVenuesAsync();
    Task<Venue?> GetVenueByIdAsync(int venueId);
    // Category event and filter category
    Task<List<EventDto>> GetEventsAsync(string? category = null);
    Task<EventDto?> GetEventByIdAsync(int id);
    Task<Event> AddEventAsync(Event ev);
}