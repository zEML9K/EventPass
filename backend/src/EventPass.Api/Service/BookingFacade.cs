using EventPass.Api.DTO;
using EventPass.Api.Entity;
using EventPass.Api.Repositories;

namespace EventPass.Api.Services;

/// <summary>
/// Фасад підсистеми бронювань і квитків: об'єднує репозиторій бронювань,
/// генерацію QR-кодів та формування DTO історії й деталей замовлення.
/// </summary>
public sealed class BookingFacade
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IQrCodeService? _qrCodeService;

    public BookingFacade(IBookingRepository bookingRepository, IQrCodeService? qrCodeService = null)
    {
        _bookingRepository = bookingRepository;
        _qrCodeService = qrCodeService;
    }

    public async Task<(bool Success, string? ErrorCode, BookingResponse? Result)> BookTicketsAsync(BookTicketRequest request)
    {
        if (request.Quantity <= 0)
            return (false, "invalid_quantity", null);

        var (success, errorCode, booking) = await _bookingRepository.CreateBookingAsync(request.EventId, request.Quantity);
        if (!success || booking == null)
            return (false, errorCode, null);

        var ticketDtos = booking.Tickets
            .Select(t => TicketMapper.ToDto(t, _qrCodeService))
            .ToList();

        return (true, null, new BookingResponse(booking.Id, booking.EventId, booking.Quantity, ticketDtos));
    }

    public async Task<(bool Success, string? ErrorCode, BookingDetailsDto? Result)> CancelBookingAsync(int bookingId)
    {
        var (success, errorCode) = await _bookingRepository.CancelBookingAsync(bookingId);
        if (!success)
            return (false, errorCode, null);

        var details = await GetBookingByIdAsync(bookingId);
        return details == null
            ? (false, "booking_not_found", null)
            : (true, null, details);
    }

    public async Task<List<BookingDetailsDto>> GetBookingsAsync()
    {
        var bookings = await _bookingRepository.GetBookingsAsync();
        return await MapBookingsWithHistoryAsync(bookings);
    }

    public async Task<BookingDetailsDto?> GetBookingByIdAsync(int bookingId)
    {
        var bookings = await _bookingRepository.GetBookingsAsync(bookingId);
        var mapped = await MapBookingsWithHistoryAsync(bookings);
        return mapped.FirstOrDefault();
    }

    public async Task<(bool Success, string? ErrorCode, TicketDto? Ticket)> CheckTicketAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return (false, "invalid_token", null);

        var normalizedToken = token.Trim();
        var (scanned, ticket) = await _bookingRepository.ScanTicketAsync(normalizedToken);

        if (ticket == null)
            return (false, "ticket_not_found", null);

        if (scanned)
            return (true, null, TicketMapper.ToDto(ticket, _qrCodeService, withQr: false));

        return ticket.Status switch
        {
            TicketStatus.Cancelled => (false, "ticket_cancelled", null),
            _ => (false, "ticket_already_used", null)
        };
    }

    private async Task<List<BookingDetailsDto>> MapBookingsWithHistoryAsync(List<Booking> bookings)
    {
        if (bookings.Count == 0)
            return new List<BookingDetailsDto>();

        var bookingIds = bookings.Select(b => b.Id).ToList();
        var historyLookup = (await _bookingRepository.GetHistoryAsync(bookingIds))
            .ToLookup(h => h.BookingId);

        return bookings.Select(b => new BookingDetailsDto(
            b.Id,
            b.EventId,
            b.Event?.Title ?? string.Empty,
            b.CreatedAt,
            b.Quantity,
            b.Status.ToString(),
            b.Tickets
                .OrderBy(t => t.Id)
                .Select(t => TicketMapper.ToDto(t, _qrCodeService, t.Status == TicketStatus.Valid))
                .ToList(),
            historyLookup[b.Id]
                .Select(h => new BookingHistoryDto(h.OldStatus?.ToString(), h.NewStatus.ToString(), h.ChangedAt))
                .ToList()
        )).ToList();
    }
}