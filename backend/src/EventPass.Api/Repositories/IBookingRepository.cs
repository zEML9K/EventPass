using EventPass.Api.Entity;

namespace EventPass.Api.Repositories;

public interface IBookingRepository
{
    // Бронювання та квитки
    Task<(bool Success, string? ErrorCode, Booking? Booking)> CreateBookingAsync(int eventId, int quantity, int? userId = null);
    Task<(bool Success, string? ErrorCode)> CancelBookingAsync(int bookingId);
    Task<List<Booking>> GetBookingsAsync(int? bookingId = null, int? userId = null);
    Task<List<BookingHistory>> GetHistoryAsync(List<int> bookingIds);
    Task<(bool Scanned, QrTicket? Ticket)> ScanTicketAsync(string token);
}