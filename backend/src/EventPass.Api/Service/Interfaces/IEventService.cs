using EventPass.Api.DTO;

namespace EventPass.Api.Services.Interfaces;

public interface IEventService
{
    Task<List<VenueDto>> GetVenuesAsync();
    Task<List<EventDto>> GetAllAsync();
    Task<(bool Success, string? ErrorCode, EventDto? Result)> GetByIdAsync(int id);
    Task<(bool Success, string? ErrorCode, EventDto? Result)> CreateEventAsync(CreateEventRequest request);
}