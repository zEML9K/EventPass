using EventPass.Api.Data;
using EventPass.Api.DTO;
using EventPass.Api.Entity;
using Microsoft.EntityFrameworkCore;

namespace EventPass.Api.Repositories;

public sealed class EfEventRepository : IEventRepository
{
    private readonly EventPassDbContext _db;

    public EfEventRepository(EventPassDbContext db) => _db = db;

    private static EventDto MapToDto(Event e)
    {
        var venueName = e.Venue?.Name ?? string.Empty;
        var venueAddress = e.Venue?.Address ?? string.Empty;

        return new EventDto(
            e.Id,
            e.Title,
            e.Description,
            e.Date,
            e.TotalTickets,
            e.AvailableSeats,
            venueName,
            venueAddress,
            e.Category,
            e.AverageRating,
            e.ReviewsCount
        );
    }

    private async Task<bool> IsDuplicateByClientAsync<TEntity>(
        IQueryable<TEntity> source,
        int eventId,
        string clientId,
        string clientIp) where TEntity : class
    {
        if (!string.IsNullOrEmpty(clientId))
        {
            return await source
                .Where(x => EF.Property<int>(x, "EventId") == eventId &&
                            EF.Property<string>(x, "ClientId") == clientId)
                .AnyAsync();
        }

        if (!string.IsNullOrEmpty(clientIp))
        {
            return await source
                .Where(x => EF.Property<int>(x, "EventId") == eventId &&
                            EF.Property<string>(x, "ClientIp") == clientIp)
                .AnyAsync();
        }

        return false;
    }

    public async Task<List<VenueDto>> GetVenuesAsync()
    {
        var venues = await _db.Venues
            .AsNoTracking()
            .OrderBy(v => v.Name)
            .ToListAsync();

        return venues
            .Select(v => new VenueDto(v.Id, v.Name, v.Address, v.Capacity))
            .ToList();
    }

    public Task<Venue?> GetVenueByIdAsync(int venueId) =>
        _db.Venues.AsNoTracking().FirstOrDefaultAsync(v => v.Id == venueId);

    public Task<bool> EventExistsAsync(int eventId) =>
        _db.Events.AsNoTracking().AnyAsync(e => e.Id == eventId);

    public async Task<List<EventDto>> GetEventsAsync(string? category = null)
    {
        var query = _db.Events
            .AsNoTracking()
            .Include(e => e.Venue)
            .AsQueryable();

        var filter = category?.Trim();
        if (!string.IsNullOrEmpty(filter))
        {
            query = query.Where(e => e.Category.ToLower() == filter.ToLower());
        }

        var events = await query.OrderBy(e => e.Date).ToListAsync();
        return events.Select(MapToDto).ToList();
    }

    public async Task<EventDto?> GetEventByIdAsync(int id)
    {
        var ev = await _db.Events
            .AsNoTracking()
            .Include(e => e.Venue)
            .FirstOrDefaultAsync(e => e.Id == id);

        return ev == null ? null : MapToDto(ev);
    }

    public async Task<Event> AddEventAsync(Event ev)
    {
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        return ev;
    }

    public async Task<(bool Success, string? ErrorCode, EventDto? UpdatedEvent)> AddReviewAsync(
        int eventId, int rating, string comment, double newAverage, int newCount, string clientId, string clientIp)
    {
        if (!await EventExistsAsync(eventId))
            return (false, "event_not_found", null);

        if (await IsDuplicateByClientAsync(_db.EventReviews.AsNoTracking(), eventId, clientId, clientIp))
            return (false, "already_rated", null);

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var ev = await _db.Events
            .Include(e => e.Venue)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (ev == null)
        {
            await transaction.RollbackAsync();
            return (false, "event_not_found", null);
        }

        _db.EventReviews.Add(new EventReview
        {
            EventId = eventId,
            Rating = rating,
            Comment = comment,
            ClientId = clientId,
            ClientIp = clientIp,
            CreatedAt = DateTime.UtcNow
        });

        ev.AverageRating = newAverage;
        ev.ReviewsCount = newCount;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (true, null, MapToDto(ev));
    }

    public Task<List<EventReviewDto>> GetReviewsAsync(int eventId) =>
        _db.EventReviews
            .AsNoTracking()
            .Where(r => r.EventId == eventId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new EventReviewDto(r.Id, r.Rating, r.Comment, r.CreatedAt))
            .ToListAsync();

    public async Task<(bool Success, string? ErrorCode)> AddReportAsync(
        int eventId, string category, string comment, string clientId, string clientIp)
    {
        if (!await EventExistsAsync(eventId))
            return (false, "event_not_found");

        if (await IsDuplicateByClientAsync(_db.EventReports.AsNoTracking(), eventId, clientId, clientIp))
            return (false, "already_reported");

        _db.EventReports.Add(new EventReport
        {
            EventId = eventId,
            Category = category,
            Comment = comment,
            ClientId = clientId,
            ClientIp = clientIp,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return (true, null);
    }
}