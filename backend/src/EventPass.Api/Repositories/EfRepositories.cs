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

    public async Task<List<VenueDto>> GetVenuesAsync()
    {
        var venues = await _db.Venues.AsNoTracking()
            .OrderBy(v => v.Name)
            .ToListAsync();

        var result = new List<VenueDto>();
        foreach (var v in venues)
        {
            result.Add(new VenueDto(v.Id, v.Name, v.Address, v.Capacity));
        }

        return result;
    }

    public Task<Venue?> GetVenueByIdAsync(int venueId)
    {
        return _db.Venues.AsNoTracking().FirstOrDefaultAsync(v => v.Id == venueId);
    }

    public async Task<List<EventDto>> GetAllAsync(string? search = null, string? category = null)
    {
        var events = await _db.Events.AsNoTracking()
            .Include(e => e.Venue)
            .OrderBy(e => e.Date)
            .ToListAsync();

        bool hasSearch = !string.IsNullOrWhiteSpace(search);
        bool hasCategory = !string.IsNullOrWhiteSpace(category);
        string searchTerm = hasSearch ? search!.Trim() : string.Empty;
        string categoryTerm = hasCategory ? category!.Trim() : string.Empty;

        var result = new List<EventDto>();
        foreach (var e in events)
        {
            if (hasSearch)
            {
                bool matchTitle = e.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
                bool matchDesc = e.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
                if (!matchTitle && !matchDesc)
                {
                    continue;
                }
            }

            if (hasCategory)
            {
                if (!e.Category.Equals(categoryTerm, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            string venueName = e.Venue != null ? e.Venue.Name : string.Empty;
            string venueAddress = e.Venue != null ? e.Venue.Address : string.Empty;

            result.Add(new EventDto(
                e.Id, e.Title, e.Description, e.Date,
                e.TotalTickets, e.AvailableSeats,
                venueName, venueAddress, e.Category));
        }

        return result;
    }

    public async Task<EventDto?> GetByIdAsync(int id)
    {
        var e = await _db.Events.AsNoTracking()
            .Include(x => x.Venue)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (e == null)
        {
            return null;
        }

        string venueName = e.Venue != null ? e.Venue.Name : string.Empty;
        string venueAddress = e.Venue != null ? e.Venue.Address : string.Empty;

        return new EventDto(
            e.Id, e.Title, e.Description, e.Date,
            e.TotalTickets, e.AvailableSeats,
            venueName, venueAddress, e.Category);
    }

    public async Task<Event> AddEventAsync(Event ev)
    {
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        return ev;
    }
}

public sealed class EfBookingRepository : IBookingRepository
{
    private readonly EventPassDbContext _db;

    public EfBookingRepository(EventPassDbContext db)
    {
        _db = db;
    }

    public async Task<(bool Success, string? ErrorCode, Booking? Booking)> CreateBookingAsync(int eventId, int quantity)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var updated = await _db.Events
            .Where(e => e.Id == eventId && e.AvailableSeats >= quantity)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.AvailableSeats, e => e.AvailableSeats - quantity));

        if (updated == 0)
        {
            var exists = await _db.Events.AnyAsync(e => e.Id == eventId);
            return (false, exists ? "insufficient_seats" : "event_not_found", null);
        }

        foreach (var entry in _db.ChangeTracker.Entries<Event>().ToList())
        {
            if (entry.Entity.Id == eventId)
            {
                await entry.ReloadAsync();
                break;
            }
        }

        var booking = new Booking
        {
            EventId = eventId,
            Quantity = quantity,
            Status = BookingStatus.Confirmed
        };

        for (int i = 0; i < quantity; i++)
        {
            booking.Tickets.Add(new QrTicket
            {
                Status = TicketStatus.Valid,
                Token = Guid.NewGuid().ToString("N")
            });
        }

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        _db.BookingHistories.Add(new BookingHistory
        {
            BookingId = booking.Id,
            OldStatus = null,
            NewStatus = BookingStatus.Confirmed,
            ChangedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (true, null, booking);
    }

    public async Task<(bool Success, string? ErrorCode)> CancelBookingAsync(int bookingId)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var bookingUpdated = await _db.Bookings
            .Where(b => b.Id == bookingId && b.Status == BookingStatus.Confirmed)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, BookingStatus.Cancelled));

        if (bookingUpdated == 0)
        {
            var exists = await _db.Bookings.AnyAsync(b => b.Id == bookingId);
            return (false, exists ? "booking_already_cancelled" : "booking_not_found");
        }

        var cancelledTickets = await _db.QrTickets
            .Where(t => t.BookingId == bookingId && t.Status == TicketStatus.Valid)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, TicketStatus.Cancelled));

        if (cancelledTickets == 0)
        {
            return (false, "booking_not_cancellable");
        }

        var booking = await _db.Bookings.AsNoTracking().FirstAsync(b => b.Id == bookingId);

        await _db.Events
            .Where(e => e.Id == booking.EventId)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.AvailableSeats, e => e.AvailableSeats + cancelledTickets));

        _db.BookingHistories.Add(new BookingHistory
        {
            BookingId = bookingId,
            OldStatus = BookingStatus.Confirmed,
            NewStatus = BookingStatus.Cancelled,
            ChangedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (true, null);
    }

    public async Task<List<BookingDetailsDto>> GetBookingsAsync(int? bookingId = null, Func<string, string>? qrGenerator = null)
    {
        var query = _db.Bookings.AsNoTracking()
            .Include(b => b.Event)
            .Include(b => b.Tickets)
            .AsQueryable();

        if (bookingId != null)
        {
            query = query.Where(b => b.Id == bookingId.Value);
        }

        var bookings = await query
            .OrderByDescending(b => b.CreatedAt)
            .ThenByDescending(b => b.Id)
            .ToListAsync();

        var ids = new List<int>();
        foreach (var b in bookings)
        {
            ids.Add(b.Id);
        }

        var allHistories = await _db.BookingHistories.AsNoTracking()
            .Where(h => ids.Contains(h.BookingId))
            .OrderBy(h => h.ChangedAt)
            .ThenBy(h => h.Id)
            .ToListAsync();

        var result = new List<BookingDetailsDto>();
        foreach (var b in bookings)
        {
            var sortedTickets = new List<QrTicket>(b.Tickets);
            sortedTickets.Sort((a, c) => a.Id.CompareTo(c.Id));

            var ticketDtos = new List<TicketDto>();
            foreach (var t in sortedTickets)
            {
                string? qr = (t.Status == TicketStatus.Valid && qrGenerator != null)
                    ? qrGenerator(t.Token)
                    : null;
                ticketDtos.Add(new TicketDto(t.Id, t.Token, t.Status.ToString(), qr));
            }

            var historyDtos = new List<BookingHistoryDto>();
            foreach (var h in allHistories)
            {
                if (h.BookingId == b.Id)
                {
                    historyDtos.Add(new BookingHistoryDto(h.OldStatus?.ToString(), h.NewStatus.ToString(), h.ChangedAt));
                }
            }

            string eventTitle = b.Event != null ? b.Event.Title : string.Empty;
            result.Add(new BookingDetailsDto(
                b.Id, b.EventId, eventTitle, b.CreatedAt,
                b.Quantity, b.Status.ToString(), ticketDtos, historyDtos));
        }

        return result;
    }

    public async Task<(bool Success, string? ErrorCode, TicketDto? Ticket)> CheckTicketAsync(string token)
    {
        var now = DateTime.UtcNow;

        var updated = await _db.QrTickets
            .Where(t => t.Token == token && t.Status == TicketStatus.Valid)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Status, TicketStatus.Scanned)
                .SetProperty(t => t.ScannedAt, (DateTime?)now));

        if (updated == 1)
        {
            foreach (var entry in _db.ChangeTracker.Entries<QrTicket>().ToList())
            {
                if (entry.Entity.Token == token)
                {
                    await entry.ReloadAsync();
                }
            }
        }

        var ticket = await _db.QrTickets.AsNoTracking().FirstOrDefaultAsync(t => t.Token == token);
        if (ticket == null)
        {
            return (false, "ticket_not_found", null);
        }

        if (updated == 1)
        {
            return (true, null, new TicketDto(ticket.Id, ticket.Token, ticket.Status.ToString()));
        }

        if (ticket.Status == TicketStatus.Cancelled)
        {
            return (false, "ticket_cancelled", null);
        }

        return (false, "ticket_already_used", null);
    }
}