using EventPass.Api.Data;
using EventPass.Api.DTO;
using EventPass.Api.Entity;
using EventPass.Api.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace EventPass.Api.Services;

public sealed class EventService
{
    private readonly IEventRepository _repo;
    
    [ActivatorUtilitiesConstructor]
    public EventService(IEventRepository repo)
    {
        _repo = repo;
    }
    
    public EventService(EventPassDbContext db) : this(new EfEventRepository(db))
    {
    }

    // Список локацій (для вибору при створенні події)
    public Task<List<VenueDto>> GetVenuesAsync()
    {
        return _repo.GetVenuesAsync();
    }

    // UC-01 + Для Lab5: список подій із пошуком за назвою/описом та фільтрацією за категорією
    public Task<List<EventDto>> GetAllAsync(string? search = null, string? category = null)
    {
        return _repo.GetAllAsync(search, category);
    }

    // UC-01: одна подія
    public async Task<(bool Success, string? ErrorCode, EventDto? Result)> GetByIdAsync(int id)
    {
        var dto = await _repo.GetByIdAsync(id);
        if (dto == null)
        {
            return (false, "event_not_found", null);
        }

        return (true, null, dto);
    }

    // UC-02: створити подію
    public async Task<(bool Success, string? ErrorCode, EventDto? Result)> CreateEventAsync(CreateEventRequest request)
    {
        // A1: коректність даних
        var title = request.Title?.Trim();
        if (string.IsNullOrWhiteSpace(title) || title.Length > 200)
        {
            return (false, "invalid_title", null);
        }

        var description = request.Description?.Trim();
        if (string.IsNullOrWhiteSpace(description))
        {
            return (false, "invalid_description", null);
        }
        
        var category = string.IsNullOrWhiteSpace(request.Category) ? "Загальне" : request.Category.Trim();
        if (category.Length > 50)
        {
            return (false, "invalid_category", null);
        }

        var date = request.Date.Kind == DateTimeKind.Local
            ? request.Date.ToUniversalTime()
            : DateTime.SpecifyKind(request.Date, DateTimeKind.Utc);
        if (date <= DateTime.UtcNow)
        {
            return (false, "invalid_date", null);
        }

        if (request.TotalTickets <= 0)
        {
            return (false, "invalid_total_tickets", null);
        }

        // Локація має існувати
        var venue = await _repo.GetVenueByIdAsync(request.VenueId);
        if (venue == null)
        {
            return (false, "venue_not_found", null);
        }

        // A2: квитків не більше, ніж місткість
        if (request.TotalTickets > venue.Capacity)
        {
            return (false, "tickets_exceed_capacity", null);
        }

        var ev = new Event
        {
            Title = title,
            Description = description,
            Category = category,
            Date = date,
            TotalTickets = request.TotalTickets,
            AvailableSeats = request.TotalTickets,
            VenueId = venue.Id
        };

        await _repo.AddEventAsync(ev);

        return (true, null, new EventDto(
            ev.Id, ev.Title, ev.Description, ev.Date,
            ev.TotalTickets, ev.AvailableSeats,
            venue.Name, venue.Address, ev.Category));
    }
}