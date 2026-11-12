using System.Linq.Expressions;
using EventPass.Api.Data;
using EventPass.Api.DTO;
using EventPass.Api.Entity;
using Microsoft.EntityFrameworkCore;
using EventPass.Api.Services.Interfaces;

namespace EventPass.Api.Services;

public sealed class EventService : IEventService
{
    private readonly EventPassDbContext _db;

    public EventService(EventPassDbContext db) => _db = db;

    private static readonly Expression<Func<Event, EventDto>> ToDto = e => new EventDto(
        e.Id, e.Title, e.Description, e.Date, e.TotalTickets, e.AvailableSeats,
        e.Venue!.Name, e.Venue!.Address);

    // Список локацій (для вибору при створенні події)
    public Task<List<VenueDto>> GetVenuesAsync() =>
        _db.Venues.AsNoTracking()
            .OrderBy(v => v.Name)
            .Select(v => new VenueDto(v.Id, v.Name, v.Address, v.Capacity))
            .ToListAsync();

    // UC-01: список подій
    public Task<List<EventDto>> GetAllAsync() =>
        _db.Events.AsNoTracking().OrderBy(e => e.Date).Select(ToDto).ToListAsync();

    // UC-01: одна подія
    public async Task<(bool Success, string? ErrorCode, EventDto? Result)> GetByIdAsync(int id)
    {
        var dto = await _db.Events.AsNoTracking().Where(e => e.Id == id).Select(ToDto).FirstOrDefaultAsync();
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

        var date = request.Date.Kind == DateTimeKind.Local
            ? request.Date.ToUniversalTime()
            : DateTime.SpecifyKind(request.Date, DateTimeKind.Utc);
        if (date <= DateTime.UtcNow)
            return (false, "invalid_date", null);

        if (request.TotalTickets <= 0)
            return (false, "invalid_total_tickets", null);

        // Локація має існувати
        var venue = await _db.Venues.AsNoTracking().FirstOrDefaultAsync(v => v.Id == request.VenueId);
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
            VenueId = venue.Id
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return (true, null, new EventDto(ev.Id, ev.Title, ev.Description, ev.Date,
            ev.TotalTickets, ev.AvailableSeats, venue.Name, venue.Address));
    }
}