namespace EventPass.Api.Entity;

public sealed class BookingHistory
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public BookingStatus OldStatus { get; set; }
    public BookingStatus NewStatus { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}