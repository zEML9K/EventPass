namespace EventPass.Api.Entity;

public sealed class EventReport
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event? Event { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientIp { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
