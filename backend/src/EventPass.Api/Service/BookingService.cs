using EventPass.Api.Data;
using EventPass.Api.DTO;
using EventPass.Api.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace EventPass.Api.Services;

public sealed class BookingService
{
    private readonly IBookingRepository _repo;
    private readonly IQrCodeService? _qr;
    
    [ActivatorUtilitiesConstructor]
    public BookingService(IBookingRepository repo, IQrCodeService? qr = null)
    {
        _repo = repo;
        _qr = qr;
    }
    
    public BookingService(EventPassDbContext db, IQrCodeService? qr = null)
        : this(new EfBookingRepository(db), qr)
    {
    }

    // UC-03: Забронювати квиток
    public async Task<(bool Success, string? ErrorCode, BookingResponse? Result)> BookTicketsAsync(BookTicketRequest request)
    {
        if (request.Quantity <= 0)
        {
            return (false, "invalid_quantity", null);
        }

        var (success, errorCode, booking) = await _repo.CreateBookingAsync(request.EventId, request.Quantity);
        if (!success || booking == null)
        {
            return (false, errorCode, null);
        }

        // Для Lab5: формування списку DTO квитків простим циклом без LINQ
        var ticketDtos = new List<TicketDto>();
        foreach (var t in booking.Tickets)
        {
            string? qrBase64 = _qr != null ? _qr.GenerateBase64Png(t.Token) : null;
            ticketDtos.Add(new TicketDto(t.Id, t.Token, t.Status.ToString(), qrBase64));
        }

        return (true, null, new BookingResponse(booking.Id, booking.EventId, booking.Quantity, ticketDtos));
    }

    // UC-05: Скасувати замовлення
    public async Task<(bool Success, string? ErrorCode, BookingDetailsDto? Result)> CancelBookingAsync(int bookingId)
    {
        var (success, errorCode) = await _repo.CancelBookingAsync(bookingId);
        if (!success)
        {
            return (false, errorCode, null);
        }

        var list = await GetBookingsAsync(bookingId);
        return (true, null, list[0]);
    }

    // Історія бронювань
    public Task<List<BookingDetailsDto>> GetBookingsAsync(int? bookingId = null)
    {
        Func<string, string>? qrGen = _qr != null ? _qr.GenerateBase64Png : null;
        return _repo.GetBookingsAsync(bookingId, qrGen);
    }

    // UC-04: Сканувати квиток контролером
    public Task<(bool Success, string? ErrorCode, TicketDto? Ticket)> CheckTicketAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult<(bool, string?, TicketDto?)>((false, "invalid_token", null));
        }

        return _repo.CheckTicketAsync(token.Trim());
    }
}