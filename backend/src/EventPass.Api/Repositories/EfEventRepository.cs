using System.Linq.Expressions;
using EventPass.Api.Data;
using EventPass.Api.DTO;
using EventPass.Api.Entity;
using Microsoft.EntityFrameworkCore;

namespace EventPass.Api.Repositories;

public sealed class EfEventRepository : IEventRepository
{
    private readonly EventPassDbContext _db;

    public EfEventRepository(EventPassDbContext db) => _db = db;

    private static readonly Expression<Func<Event, EventDto>> ToDto = e => new EventDto(
        e.Id, e.Title, e.Description, e.Date, e.TotalTickets, e.AvailableSeats,
        e.Venue!.Name, e.Venue!.Address);

    public Task<List<VenueDto>> GetVenuesAsync() =>
        _db.Venues.AsNoTracking()
            .OrderBy(v => v.Name)
            .Select(v => new VenueDto(v.Id, v.Name, v.Address, v.Capacity))
            .ToListAsync();

    public Task<Venue?> GetVenueByIdAsync(int venueId) =>
        _db.Venues.AsNoTracking().FirstOrDefaultAsync(v => v.Id == venueId);

    public Task<List<EventDto>> GetEventsAsync() =>
        _db.Events.AsNoTracking().OrderBy(e => e.Date).Select(ToDto).ToListAsync();

    public Task<EventDto?> GetEventByIdAsync(int id) =>
        _db.Events.AsNoTracking().Where(e => e.Id == id).Select(ToDto).FirstOrDefaultAsync();

    public async Task<Event> AddEventAsync(Event ev)
    {
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        return ev;
    }
}