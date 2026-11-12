using EventPass.Api.Entity;

namespace EventPass.Api.Repositories;

public sealed class InMemoryStorage
{
    public object SyncLock { get; } = new();
    public List<Venue> Venues { get; } = new();
    public List<Event> Events { get; } = new();
    public List<Booking> Bookings { get; } = new();
    public List<QrTicket> QrTickets { get; } = new();
    public List<BookingHistory> BookingHistories { get; } = new();

    private int _venueSeq = 1;
    private int _eventSeq = 1;
    private int _bookingSeq = 1;
    private int _ticketSeq = 1;
    private int _historySeq = 1;

    public InMemoryStorage()
    {
        var venue = new Venue
        {
            Id = NextVenueId(),
            Name = "Артпростір «Вежа»",
            Address = "КПІ Корпус №1, Берестейський проспект, 37",
            Capacity = 25
        };
        Venues.Add(venue);

        Events.Add(new Event
        {
            Id = NextEventId(),
            Title = "Програма Кіноекзорцизм",
            Description = "Перегляд психологічного трилера «Мати Іоанна від янголів»",
            Category = "Кіно",
            Date = DateTime.UtcNow.AddDays(7),
            TotalTickets = 25,
            AvailableSeats = 25,
            VenueId = venue.Id,
            Venue = venue
        });
    }

    public int NextVenueId() => _venueSeq++;
    public int NextEventId() => _eventSeq++;
    public int NextBookingId() => _bookingSeq++;
    public int NextTicketId() => _ticketSeq++;
    public int NextHistoryId() => _historySeq++;
}