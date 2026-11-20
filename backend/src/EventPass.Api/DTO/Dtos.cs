namespace EventPass.Api.DTO;

public record CreateEventRequest(
    string Title,
    string Description,
    DateTime Date,
    int TotalTickets,
    int VenueId,
    string? Category = null);

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
    string Category = "",
    double AverageRating = 0,
    int ReviewsCount = 0);

public record RateEventRequest(int Rating, string? Comment = null, string? ClientId = null);
public record EventReviewDto(int Id, int Rating, string Comment, DateTime CreatedAt);

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

public record CreateReportRequest(string Category, string? Comment = null, string? ClientId = null);
public record EventReportDto(int Id, string Category, string Comment, DateTime CreatedAt);

public record RegisterRequest(string Username, string Password, string Role);
public record LoginRequest(string Username, string Password, string Role);
public record AuthResponse(int UserId, string Username, string Role, string ClientId, string Token);

public record UserProfileDto(int UserId, string Username, string Role, string ClientId, DateTime CreatedAt);
public record UserReviewDto(int Id, int EventId, string EventTitle, int Rating, string Comment, DateTime CreatedAt);
public record UserReportDto(int Id, int EventId, string EventTitle, string Category, string Comment, DateTime CreatedAt);
public record UserActivityDto(
    UserProfileDto Profile,
    List<BookingDetailsDto> Bookings,
    List<UserReviewDto> Reviews,
    List<UserReportDto> Reports);