namespace EventPass.Api.Entity;

public sealed class Booking
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int Quantity { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;

    public int EventId { get; set; }
    public Event? Event { get; set; }

    public List<QrTicket> Tickets { get; set; } = new();
}