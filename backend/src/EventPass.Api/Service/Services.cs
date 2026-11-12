using EventPass.Api.Entity;
using EventPass.Api.DTO;
using EventPass.Api.Repositories;

namespace EventPass.Api.Services;

public sealed class BookingService
{
    private readonly IBookingRepository _repo;
    private readonly IQrCodeService? _qr;

    public BookingService(IBookingRepository repo, IQrCodeService? qr = null)
    {
        _repo = repo;
        _qr = qr;
    }

    // UC-03: Забронювати квиток
    public async Task<(bool Success, string? ErrorCode, BookingResponse? Result)> BookTicketsAsync(BookTicketRequest request)
    {
        if (request.Quantity <= 0)
            return (false, "invalid_quantity", null);

        var (success, errorCode, booking) = await _repo.CreateBookingAsync(request.EventId, request.Quantity);
        if (!success || booking == null)
            return (false, errorCode, null);

        var ticketDtos = booking.Tickets
            .Select(t => TicketMapper.ToDto(t, _qr))
            .ToList();;

        return (true, null, new BookingResponse(booking.Id, booking.EventId, booking.Quantity, ticketDtos));
    }

    // UC-05: Скасувати замовлення
    public async Task<(bool Success, string? ErrorCode, BookingDetailsDto? Result)> CancelBookingAsync(int bookingId)
    {
        var (success, errorCode) = await _repo.CancelBookingAsync(bookingId);
        if (!success)
            return (false, errorCode, null);

        var details = (await GetBookingsAsync(bookingId)).Single();
        return (true, null, details);
    }

    // Історія бронювань: бронювання з квитками та історією змін статусу
    public async Task<List<BookingDetailsDto>> GetBookingsAsync(int? bookingId = null)
    {
        var bookings = await _repo.GetBookingsAsync(bookingId);

        var ids = bookings.Select(b => b.Id).ToList();
        var history = (await _repo.GetHistoryAsync(ids)).ToLookup(h => h.BookingId);

        return bookings.Select(b => new BookingDetailsDto(
            b.Id,
            b.EventId,
            b.Event?.Title ?? string.Empty,
            b.CreatedAt,
            b.Quantity,
            b.Status.ToString(),
            b.Tickets.OrderBy(t => t.Id)
                .Select(t => TicketMapper.ToDto(t, _qr, t.Status == TicketStatus.Valid))
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

        var (scanned, ticket) = await _repo.ScanTicketAsync(token);
        if (ticket == null)
            return (false, "ticket_not_found", null);

        if (scanned)
            return (true, null, TicketMapper.ToDto(ticket, _qr, withQr: false));

        return ticket.Status switch
        {
            TicketStatus.Cancelled => (false, "ticket_cancelled", null),
            _ => (false, "ticket_already_used", null)
        };
    }
}