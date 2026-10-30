namespace EventPass.Api.Entity;

public sealed class QrTicket
{
    public int Id { get; set; }
    public string Token { get; set; } = Guid.NewGuid().ToString("N");
    public TicketStatus Status { get; set; } = TicketStatus.Valid;
    public DateTime? ScannedAt { get; set; }

    public int BookingId { get; set; }
    public Booking? Booking { get; set; }
}