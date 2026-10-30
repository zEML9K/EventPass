using EventPass.Api.DTO;
using EventPass.Api.Data;
using EventPass.Api.Entity;
using Microsoft.EntityFrameworkCore;

namespace EventPass.Api.Services;

public sealed class BookingService
{
    private readonly EventPassDbContext _db;

    public BookingService(EventPassDbContext db)
    {
        _db = db;
    }

    // UC-03: Забронювати квиток
    public async Task<(bool Success, string? ErrorCode, BookingResponse? Result)> BookTicketsAsync(BookTicketRequest request)
    {
        if (request.Quantity <= 0)
            return (false, "invalid_quantity", null);
        
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == request.EventId);
        if (ev == null)
            return (false, "event_not_found", null);

        if (ev.AvailableSeats < request.Quantity)
            return (false, "insufficient_seats", null);
        
        ev.AvailableSeats -= request.Quantity;
        
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
            OldStatus = BookingStatus.Confirmed,
            NewStatus = BookingStatus.Confirmed,
            ChangedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        var ticketDtos = booking.Tickets
            .Select(t => new TicketDto(t.Id, t.Token, t.Status.ToString()))
            .ToList();

        return (true, null, new BookingResponse(booking.Id, booking.EventId, booking.Quantity, ticketDtos));
    }

    // UC-04: Перевірити/сканувати квиток контролером
    public async Task<(bool Success, string? ErrorCode, TicketDto? Ticket)> CheckTicketAsync(string token)
    {
        var ticket = await _db.QrTickets.FirstOrDefaultAsync(t => t.Token == token);
        if (ticket == null)
            return (false, "ticket_not_found", null);

        if (ticket.Status == TicketStatus.Scanned)
            return (false, "ticket_already_used", null);

        if (ticket.Status == TicketStatus.Cancelled)
            return (false, "ticket_cancelled", null);
        
        ticket.Status = TicketStatus.Scanned;
        ticket.ScannedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return (true, null, new TicketDto(ticket.Id, ticket.Token, ticket.Status.ToString()));
    }
}