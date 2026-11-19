using EventPass.Api.DTO;
using EventPass.Api.Entity;

namespace EventPass.Api.Repositories;

public interface IEventRepository
{
    Task<List<VenueDto>> GetVenuesAsync();
    Task<Venue?> GetVenueByIdAsync(int venueId);
    Task<bool> EventExistsAsync(int eventId);
    Task<List<EventDto>> GetEventsAsync(string? category = null);
    Task<EventDto?> GetEventByIdAsync(int id);
    Task<Event> AddEventAsync(Event ev);

    Task<(bool Success, string? ErrorCode, EventDto? UpdatedEvent)> AddReviewAsync(
        int eventId, int rating, string comment, double newAverage, int newCount, string clientId, string clientIp);
    Task<List<EventReviewDto>> GetReviewsAsync(int eventId);

    Task<(bool Success, string? ErrorCode)> AddReportAsync(
        int eventId, string category, string comment, string clientId, string clientIp);
}