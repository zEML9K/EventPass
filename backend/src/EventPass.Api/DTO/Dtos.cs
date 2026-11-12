namespace EventPass.Api.DTO;

public record CreateEventRequest(
    string Title,
    string Description,
    DateTime Date,
    int TotalTickets,
    int VenueId,
    // Для Lab5: категорія події для фільтрації та початкової міграції БД
    string Category = "Загальне");

public record BookTicketRequest(int EventId, int Quantity);
public record CheckTicketRequest(string Token);

public record EventDto(
    int Id,
    string Title,
    string Description,
    DateTime Date,
    int TotalTickets,
    int AvailableSeats,
    string VenueName,
    string VenueAddress,
    // Для Lab5: передача категорії події клієнту
    string Category = "Загальне");

public record TicketDto(int Id, string Token, string Status, string? QrCodeBase64 = null);
public record BookingResponse(int BookingId, int EventId, int Quantity, List<TicketDto> Tickets);

public record VenueDto(int Id, string Name, string Address, int Capacity);

public record BookingHistoryDto(string? OldStatus, string NewStatus, DateTime ChangedAt);
public record BookingDetailsDto(
    int BookingId,
    int EventId,
    string EventTitle,
    DateTime CreatedAt,
    int Quantity,
    string Status,
    List<TicketDto> Tickets,
    List<BookingHistoryDto> History);

public record ApiError(string Code, string Message);