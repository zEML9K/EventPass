using EventPass.Api.DTO;
using EventPass.Api.Data;
using EventPass.Api.Entity;
using EventPass.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventPass.Tests;

public class BookingServiceTests
{
    private EventPassDbContext CreateInMemoryDb()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<EventPassDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new EventPassDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    // 1. УСПІШНЕ БРОНЮВАННЯ (FR-03, FR-04, FR-06)
    [Fact]
    public async Task BookTickets_ShouldSucceed_WhenSeatsAreAvailable()
    {
        var db = CreateInMemoryDb();
        var service = new BookingService(db);

        var venue = new Venue { Name = "Main Hall", Capacity = 100 };
        db.Venues.Add(venue);
        await db.SaveChangesAsync();

        var ev = new Event { Title = "IT Conf", TotalTickets = 10, AvailableSeats = 10, VenueId = venue.Id };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        
        var request = new BookTicketRequest(EventId: ev.Id, Quantity: 3);
        var result = await service.BookTicketsAsync(request);
        
        Assert.True(result.Success);
        Assert.NotNull(result.Result);
        Assert.Equal(3, result.Result.Quantity);
        Assert.Equal(3, result.Result.Tickets.Count);
        
        var dbEvent = await db.Events.FindAsync(ev.Id);
        Assert.Equal(7, dbEvent!.AvailableSeats);
        
        var history = await db.BookingHistories.FirstOrDefaultAsync();
        Assert.NotNull(history);
    }

    // 2. ВІДМОВА ПРИ БРАКУ МІСЦЬ (FR-03 / NFR-02)
    [Fact]
    public async Task BookTickets_ShouldFail_WhenNotEnoughSeats()
    {
        var db = CreateInMemoryDb();
        var service = new BookingService(db);

        var venue = new Venue { Name = "Main Hall", Capacity = 100 };
        db.Venues.Add(venue);
        await db.SaveChangesAsync();

        var ev = new Event { Title = "Концерт", TotalTickets = 10, AvailableSeats = 2, VenueId = venue.Id };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        
        var request = new BookTicketRequest(EventId: ev.Id, Quantity: 5);
        var result = await service.BookTicketsAsync(request);
        
        Assert.False(result.Success);
        Assert.Equal("insufficient_seats", result.ErrorCode);
        
        var dbEvent = await db.Events.FindAsync(ev.Id);
        Assert.Equal(2, dbEvent!.AvailableSeats);
    }

    // 3. УНІКАЛЬНІСТЬ ТОКЕНІВ ТА ПОЧАТКОВИЙ СТАТУС VALID (FR-04)
    [Fact]
    public async Task BookTickets_ShouldGenerateUniqueTokens_WithValidStatus()
    {
        var db = CreateInMemoryDb();
        var service = new BookingService(db);

        var venue = new Venue { Name = "Main Hall", Capacity = 100 };
        db.Venues.Add(venue);
        await db.SaveChangesAsync();

        var ev = new Event { Title = "Show", TotalTickets = 10, AvailableSeats = 10, VenueId = venue.Id };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        
        var result = await service.BookTicketsAsync(new BookTicketRequest(ev.Id, 2));
        
        var tickets = result.Result!.Tickets;
        Assert.Equal("Valid", tickets[0].Status);
        Assert.Equal("Valid", tickets[1].Status);
        Assert.NotEqual(tickets[0].Token, tickets[1].Token);
    }

    // 4. УСПІШНА ПЕРЕВІРКА ВАЛІДНОГО КВИТКА (FR-05)
    [Fact]
    public async Task CheckTicket_ShouldSucceed_AndChangeStatusToScanned()
    {
        var db = CreateInMemoryDb();
        var service = new BookingService(db);

        var venue = new Venue { Name = "Hall", Capacity = 100 };
        db.Venues.Add(venue);
        await db.SaveChangesAsync();

        var ev = new Event { Title = "Event", TotalTickets = 10, AvailableSeats = 10, VenueId = venue.Id };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var booking = new Booking { EventId = ev.Id, Quantity = 1, Status = BookingStatus.Confirmed };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var ticket = new QrTicket { Token = "valid-token-777", Status = TicketStatus.Valid, BookingId = booking.Id };
        db.QrTickets.Add(ticket);
        await db.SaveChangesAsync();
        
        var result = await service.CheckTicketAsync("valid-token-777");
        
        Assert.True(result.Success);
        Assert.Equal("Scanned", result.Ticket!.Status);
        
        var dbTicket = await db.QrTickets.FirstAsync(t => t.Token == "valid-token-777");
        Assert.Equal(TicketStatus.Scanned, dbTicket.Status);
        Assert.NotNull(dbTicket.ScannedAt);
    }

    // 5. ВІДМОВА ПРИ ПОВТОРНОМУ СКУНУВАННІ (FR-05)
    [Fact]
    public async Task CheckTicket_ShouldFail_WhenTicketIsAlreadyScanned()
    {
        var db = CreateInMemoryDb();
        var service = new BookingService(db);

        var venue = new Venue { Name = "Hall", Capacity = 100 };
        db.Venues.Add(venue);
        await db.SaveChangesAsync();

        var ev = new Event { Title = "Event", TotalTickets = 10, AvailableSeats = 10, VenueId = venue.Id };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var booking = new Booking { EventId = ev.Id, Quantity = 1, Status = BookingStatus.Confirmed };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var ticket = new QrTicket { Token = "my-qr-code-123", Status = TicketStatus.Scanned, BookingId = booking.Id };
        db.QrTickets.Add(ticket);
        await db.SaveChangesAsync();
        
        var result = await service.CheckTicketAsync("my-qr-code-123");
        
        Assert.False(result.Success);
        Assert.Equal("ticket_already_used", result.ErrorCode);
    }

    // 6. ВІДМОВА, ЯКЩО КВИТОК НЕ ЗНАЙДЕНО (FR-05)
    [Fact]
    public async Task CheckTicket_ShouldFail_WhenTokenNotFound()
    {
        var db = CreateInMemoryDb();
        var service = new BookingService(db);
        
        var result = await service.CheckTicketAsync("non-existent-token");
        
        Assert.False(result.Success);
        Assert.Equal("ticket_not_found", result.ErrorCode);
    }
}