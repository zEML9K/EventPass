namespace EventPass.Api.Entity;

public sealed class Event
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    // Для Lab5: можливість фільтрації та початкова міграція БД
    public string Category { get; set; } = "Загальне";
    public DateTime Date { get; set; }
    public int TotalTickets { get; set; }
    public int AvailableSeats { get; set; }

    public int VenueId { get; set; }
    public Venue? Venue { get; set; }
}