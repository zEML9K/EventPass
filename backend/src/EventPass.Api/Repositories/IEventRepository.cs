using EventPass.Api.DTO;
using EventPass.Api.Entity;

namespace EventPass.Api.Repositories;

public interface IEventRepository
{
    Task<List<VenueDto>> GetVenuesAsync();
    Task<Venue?> GetVenueByIdAsync(int venueId);
    Task<List<EventDto>> GetAllAsync(string? search = null, string? category = null);
    Task<EventDto?> GetByIdAsync(int id);
    Task<Event> AddEventAsync(Event ev);
}