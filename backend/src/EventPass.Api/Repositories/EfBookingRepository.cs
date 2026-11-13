using EventPass.Api.Data;
using EventPass.Api.Entity;
using Microsoft.EntityFrameworkCore;

namespace EventPass.Api.Repositories;

public sealed class EfBookingRepository : IBookingRepository
{
    private readonly EventPassDbContext _db;

    public EfBookingRepository(EventPassDbContext db) => _db = db;

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
            return (false, "booking_not_cancellable");

        var eventId = await _db.Bookings
            .Where(b => b.Id == bookingId)
            .Select(b => b.EventId)
            .SingleAsync();

        await _db.Events
            .Where(e => e.Id == eventId)
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

    public Task<List<Booking>> GetBookingsAsync(int? bookingId = null)
    {
        var query = _db.Bookings.AsNoTracking()
            .Include(b => b.Event)
            .Include(b => b.Tickets)
            .AsQueryable();

        if (bookingId != null)
            query = query.Where(b => b.Id == bookingId);

        return query
            .OrderByDescending(b => b.CreatedAt)
            .ThenByDescending(b => b.Id)
            .ToListAsync();
    }

    public async Task<List<BookingHistory>> GetHistoryAsync(List<int> bookingIds)
    {
        return (await _db.BookingHistories.AsNoTracking()
                .Where(h => bookingIds.Contains(h.BookingId))
                .ToListAsync())
            .OrderBy(h => h.ChangedAt)
            .ThenBy(h => h.Id)
            .ToList();
    }

    public async Task<(bool Scanned, QrTicket? Ticket)> ScanTicketAsync(string token)
    {
        var now = DateTime.UtcNow;

        var updated = await _db.QrTickets
            .Where(t => t.Token == token && t.Status == TicketStatus.Valid)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Status, TicketStatus.Scanned)
                .SetProperty(t => t.ScannedAt, (DateTime?)now));

        if (updated == 1)
        {
            foreach (var entry in _db.ChangeTracker.Entries<QrTicket>().Where(e => e.Entity.Token == token).ToList())
                await entry.ReloadAsync();
        }

        var ticket = await _db.QrTickets.AsNoTracking().FirstOrDefaultAsync(t => t.Token == token);
        return (updated == 1, ticket);
    }
}