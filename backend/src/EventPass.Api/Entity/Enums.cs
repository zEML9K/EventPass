namespace EventPass.Api.Entity;

public enum UserRole
{
    Visitor,
    Organizer,
    Controller
}

public enum BookingStatus
{
    Confirmed,
    Cancelled
}

public enum TicketStatus
{
    Valid,
    Scanned,
    Cancelled
}