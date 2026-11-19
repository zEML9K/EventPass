using EventPass.Api.Data;
using EventPass.Api.DTO;
using EventPass.Api.Entity;
using Microsoft.EntityFrameworkCore;

namespace EventPass.Api.Repositories;

public sealed class EfEventRepository : IEventRepository
{
    private readonly EventPassDbContext _db;

    public EfEventRepository(EventPassDbContext db)
    {
        _db = db;
    }

    private static EventDto MapToDto(Event e)
    {
        string venueName = e.Venue != null ? e.Venue.Name : string.Empty;
        string venueAddress = e.Venue != null ? e.Venue.Address : string.Empty;

        return new EventDto(
            e.Id,
            e.Title,
            e.Description,
            e.Date,
            e.TotalTickets,
            e.AvailableSeats,
            venueName,
            venueAddress,
            e.Category
        );
    }

    public async Task<List<VenueDto>> GetVenuesAsync()
    {
        List<Venue> venues = await _db.Venues
            .AsNoTracking()
            .OrderBy(v => v.Name)
            .ToListAsync();

        var result = new List<VenueDto>();
        foreach (Venue v in venues)
        {
            result.Add(new VenueDto(v.Id, v.Name, v.Address, v.Capacity));
        }

        return result;
    }

    public async Task<Venue?> GetVenueByIdAsync(int venueId)
    {
        return await _db.Venues
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == venueId);
    }

    // Category event and filter category
    public async Task<List<EventDto>> GetEventsAsync(string? category = null)
    {
        List<Event> allEvents = await _db.Events
            .AsNoTracking()
            .Include(e => e.Venue)
            .OrderBy(e => e.Date)
            .ToListAsync();

        string? filter = category?.Trim();
        bool hasFilter = !string.IsNullOrEmpty(filter);

        var result = new List<EventDto>();
        foreach (Event ev in allEvents)
        {
            if (hasFilter && !string.Equals(ev.Category, filter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result.Add(MapToDto(ev));
        }

        return result;
    }

    public async Task<EventDto?> GetEventByIdAsync(int id)
    {
        Event? ev = await _db.Events
            .AsNoTracking()
            .Include(e => e.Venue)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (ev == null)
        {
            return null;
        }

        return MapToDto(ev);
    }

    public async Task<Event> AddEventAsync(Event ev)
    {
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        return ev;
    }
}