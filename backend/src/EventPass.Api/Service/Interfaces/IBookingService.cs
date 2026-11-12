using EventPass.Api.DTO;

namespace EventPass.Api.Services.Interfaces;

public interface IBookingService
{
    Task<(bool Success, string? ErrorCode, BookingResponse? Result)> BookTicketsAsync(BookTicketRequest request);
    Task<(bool Success, string? ErrorCode, BookingDetailsDto? Result)> CancelBookingAsync(int bookingId);
    Task<List<BookingDetailsDto>> GetBookingsAsync(int? bookingId = null);
    Task<(bool Success, string? ErrorCode, TicketDto? Ticket)> CheckTicketAsync(string? token);
}