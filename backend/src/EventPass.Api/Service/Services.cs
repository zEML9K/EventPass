using EventPass.Api.DTO;
using EventPass.Api.Data;
using EventPass.Api.Entity;
using Microsoft.EntityFrameworkCore;

namespace EventPass.Api.Services;

public sealed class BookingService
{
    private readonly EventPassDbContext _db;
    private readonly IQrCodeService? _qr;

    public BookingService(EventPassDbContext db, IQrCodeService? qr = null)
    {
        _db = db;
        _qr = qr;
    }

    // UC-03: Забронювати квиток
    public async Task<(bool Success, string? ErrorCode, BookingResponse? Result)> BookTicketsAsync(BookTicketRequest request)
    {
        if (request.Quantity <= 0)
            return (false, "invalid_quantity", null);

        await using var transaction = await _db.Database.BeginTransactionAsync();

        // Атомарне "перевірити й списати": один UPDATE ... WHERE AvailableSeats >= @q.
        // Паралельні запити не можуть списати більше місць, ніж є (NFR-02).
        // Це перша операція транзакції, тому блокування запису береться одразу.
        var quantity = request.Quantity;
        var updated = await _db.Events
            .Where(e => e.Id == request.EventId && e.AvailableSeats >= quantity)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.AvailableSeats, e => e.AvailableSeats - quantity));

        if (updated == 0)
        {
            var exists = await _db.Events.AnyAsync(e => e.Id == request.EventId);
            return (false, exists ? "insufficient_seats" : "event_not_found", null);
        }

        // ExecuteUpdate оминає change tracker — оновлюємо вже завантажену подію
        var tracked = _db.ChangeTracker.Entries<Event>().FirstOrDefault(e => e.Entity.Id == request.EventId);
        if (tracked != null) await tracked.ReloadAsync();

        var booking = new Booking
        {
            EventId = request.EventId,
            Quantity = request.Quantity,
            Status = BookingStatus.Confirmed
        };

        for (int i = 0; i < request.Quantity; i++)
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

        var ticketDtos = booking.Tickets
            .Select(t => new TicketDto(t.Id, t.Token, t.Status.ToString(), _qr?.GenerateBase64Png(t.Token)))
            .ToList();

        return (true, null, new BookingResponse(booking.Id, booking.EventId, booking.Quantity, ticketDtos));
    }

    // UC-05: Скасувати замовлення
    // Квитки Valid -> Cancelled (кінцевий стан), місця за скасованими квитками повертаються в продаж.
    // Уже використані (Scanned) квитки не змінюються й місць не повертають.
    public async Task<(bool Success, string? ErrorCode, BookingDetailsDto? Result)> CancelBookingAsync(int bookingId)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var bookingUpdated = await _db.Bookings
            .Where(b => b.Id == bookingId && b.Status == BookingStatus.Confirmed)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, BookingStatus.Cancelled));

        if (bookingUpdated == 0)
        {
            var exists = await _db.Bookings.AnyAsync(b => b.Id == bookingId);
            return (false, exists ? "booking_already_cancelled" : "booking_not_found", null);
        }

        // Лише Valid -> Cancelled; квиток, який контролер вже відсканував, лишається Scanned
        var cancelledTickets = await _db.QrTickets
            .Where(t => t.BookingId == bookingId && t.Status == TicketStatus.Valid)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, TicketStatus.Cancelled));

        if (cancelledTickets == 0)
            return (false, "booking_not_cancellable", null);

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

        var details = (await GetBookingsAsync(bookingId)).Single();
        return (true, null, details);
    }

    // Історія бронювань: бронювання з квитками та історією змін статусу
    public async Task<List<BookingDetailsDto>> GetBookingsAsync(int? bookingId = null)
    {
        var query = _db.Bookings.AsNoTracking()
            .Include(b => b.Event)
            .Include(b => b.Tickets)
            .AsQueryable();

        if (bookingId != null)
            query = query.Where(b => b.Id == bookingId);

        var bookings = await query
            .OrderByDescending(b => b.CreatedAt)
            .ThenByDescending(b => b.Id)
            .ToListAsync();

        var ids = bookings.Select(b => b.Id).ToList();
        var history = (await _db.BookingHistories.AsNoTracking()
                .Where(h => ids.Contains(h.BookingId))
                .ToListAsync())
            .OrderBy(h => h.ChangedAt)
            .ThenBy(h => h.Id)
            .ToLookup(h => h.BookingId);

        return bookings.Select(b => new BookingDetailsDto(
            b.Id,
            b.EventId,
            b.Event?.Title ?? string.Empty,
            b.CreatedAt,
            b.Quantity,
            b.Status.ToString(),
            b.Tickets.OrderBy(t => t.Id)
                .Select(t => new TicketDto(
                    t.Id, t.Token, t.Status.ToString(),
                    t.Status == TicketStatus.Valid ? _qr?.GenerateBase64Png(t.Token) : null))
                .ToList(),
            history[b.Id]
                .Select(h => new BookingHistoryDto(h.OldStatus?.ToString(), h.NewStatus.ToString(), h.ChangedAt))
                .ToList()
        )).ToList();
    }

    // UC-04: Сканувати квиток контролером
    public async Task<(bool Success, string? ErrorCode, TicketDto? Ticket)> CheckTicketAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return (false, "invalid_token", null);

        token = token.Trim();
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
        if (ticket == null)
            return (false, "ticket_not_found", null);

        if (updated == 1)
            return (true, null, new TicketDto(ticket.Id, ticket.Token, ticket.Status.ToString()));

        return ticket.Status switch
        {
            TicketStatus.Cancelled => (false, "ticket_cancelled", null),
            _ => (false, "ticket_already_used", null)
        };
    }
}