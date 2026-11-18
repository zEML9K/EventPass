namespace EventPass.Api.Entity;

public sealed class EventReview
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event? Event { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientIp { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
