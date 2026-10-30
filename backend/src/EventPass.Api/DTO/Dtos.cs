namespace EventPass.Api.DTO;

public record CreateEventRequest(string Title, string Description, DateTime Date, int TotalTickets, int VenueId);
public record BookTicketRequest(int EventId, int Quantity);
public record CheckTicketRequest(string Token);

public record EventDto(int Id, string Title, string Description, DateTime Date, int TotalTickets, int AvailableSeats);
public record TicketDto(int Id, string Token, string Status);
public record BookingResponse(int BookingId, int EventId, int Quantity, List<TicketDto> Tickets);

public record ApiError(string Code, string Message);