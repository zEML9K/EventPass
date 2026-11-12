using EventPass.Api.DTO;
using EventPass.Api.Entity;

namespace EventPass.Api.Repositories;

public interface IBookingRepository
{
    Task<(bool Success, string? ErrorCode, Booking? Booking)> CreateBookingAsync(int eventId, int quantity);
    Task<(bool Success, string? ErrorCode)> CancelBookingAsync(int bookingId);
    Task<List<BookingDetailsDto>> GetBookingsAsync(int? bookingId = null, Func<string, string>? qrGenerator = null);
    Task<(bool Success, string? ErrorCode, TicketDto? Ticket)> CheckTicketAsync(string token);
}