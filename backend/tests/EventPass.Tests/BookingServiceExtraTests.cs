using EventPass.Api.Data;
using EventPass.Api.DTO;
using EventPass.Api.Entity;
using EventPass.Api.Repositories;
using EventPass.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventPass.Tests;

public class BookingServiceExtraTests
{
    private static EventPassDbContext CreateDb()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        var db = new EventPassDbContext(new DbContextOptionsBuilder<EventPassDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        return db;
    }

    private static BookingService NewService(EventPassDbContext db, IQrCodeService? qr = null) =>
        new(new EfBookingRepository(db), qr);

    [Fact]
    public async Task BookTickets_ShouldFail_WhenQuantityInvalid()
    {
        var result = await NewService(CreateDb()).BookTicketsAsync(new BookTicketRequest(1, 0));
        Assert.Equal("invalid_quantity", result.ErrorCode);
    }

    [Fact]
    public async Task BookTickets_ShouldFail_WhenEventNotFound()
    {
        var result = await NewService(CreateDb()).BookTicketsAsync(new BookTicketRequest(999, 1));
        Assert.Equal("event_not_found", result.ErrorCode);
    }

    [Fact]
    public async Task BookTickets_ShouldNotCreateHistory_WhenSeatsInsufficient()
    {
        var db = CreateDb();
        var venue = new Venue { Name = "H", Capacity = 10 };
        db.Venues.Add(venue);
        await db.SaveChangesAsync();
        var ev = new Event { Title = "E", TotalTickets = 1, AvailableSeats = 1, VenueId = venue.Id };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var result = await NewService(db).BookTicketsAsync(new BookTicketRequest(ev.Id, 2));

        Assert.Equal("insufficient_seats", result.ErrorCode);
        Assert.Empty(db.Bookings);
        Assert.Empty(db.BookingHistories);
    }

    [Fact]
    public async Task BookTickets_ShouldReturnQrCode_WhenGeneratorProvided()
    {
        var db = CreateDb();
        var venue = new Venue { Name = "H", Capacity = 10 };
        db.Venues.Add(venue);
        await db.SaveChangesAsync();
        var ev = new Event { Title = "E", TotalTickets = 5, AvailableSeats = 5, VenueId = venue.Id };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var result = await NewService(db, new QrCodeService()).BookTicketsAsync(new BookTicketRequest(ev.Id, 1));

        Assert.False(string.IsNullOrEmpty(result.Result!.Tickets[0].QrCodeBase64));
        var history = await db.BookingHistories.SingleAsync();
        Assert.Null(history.OldStatus);
    }

    [Fact]
    public async Task GetBookings_ShouldReturnBookingWithTicketsAndHistory()
    {
        var db = CreateDb();
        var venue = new Venue { Name = "H", Capacity = 10 };
        db.Venues.Add(venue);
        await db.SaveChangesAsync();
        var ev = new Event { Title = "E", TotalTickets = 5, AvailableSeats = 5, VenueId = venue.Id };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var service = NewService(db);
        await service.BookTicketsAsync(new BookTicketRequest(ev.Id, 2));

        var list = await service.GetBookingsAsync();

        var b = Assert.Single(list);
        Assert.Equal("E", b.EventTitle);
        Assert.Equal(2, b.Tickets.Count);
        var h = Assert.Single(b.History);
        Assert.Null(h.OldStatus);
        Assert.Equal("Confirmed", h.NewStatus);
    }

    [Fact]
    public async Task GetBookings_ShouldIncludeQrOnlyForValidTickets()
    {
        var db = CreateDb();
        var venue = new Venue { Name = "H", Capacity = 10 };
        db.Venues.Add(venue);
        await db.SaveChangesAsync();
        var ev = new Event { Title = "E", TotalTickets = 5, AvailableSeats = 5, VenueId = venue.Id };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var service = NewService(db, new QrCodeService());
        var booked = await service.BookTicketsAsync(new BookTicketRequest(ev.Id, 2));
        await service.CheckTicketAsync(booked.Result!.Tickets[0].Token); // перший квиток -> Scanned

        var tickets = (await service.GetBookingsAsync()).Single().Tickets;

        Assert.Null(tickets.Single(t => t.Status == "Scanned").QrCodeBase64);
        Assert.False(string.IsNullOrEmpty(tickets.Single(t => t.Status == "Valid").QrCodeBase64));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CheckTicket_ShouldReturnInvalidToken_WhenEmpty(string? token)
    {
        var result = await NewService(CreateDb()).CheckTicketAsync(token);
        Assert.Equal("invalid_token", result.ErrorCode);
    }

    [Fact]
    public async Task CheckTicket_ShouldFail_WhenTicketCancelled()
    {
        var db = CreateDb();
        var venue = new Venue { Name = "H", Capacity = 10 };
        db.Venues.Add(venue);
        await db.SaveChangesAsync();
        var ev = new Event { Title = "E", TotalTickets = 1, AvailableSeats = 1, VenueId = venue.Id };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        var booking = new Booking { EventId = ev.Id, Quantity = 1 };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        db.QrTickets.Add(new QrTicket { Token = "c1", Status = TicketStatus.Cancelled, BookingId = booking.Id });
        await db.SaveChangesAsync();

        var result = await NewService(db).CheckTicketAsync("c1");
        Assert.Equal("ticket_cancelled", result.ErrorCode);
    }
}