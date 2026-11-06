using EventPass.Api.Data;
using EventPass.Api.DTO;
using EventPass.Api.Entity;
using EventPass.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventPass.Tests;

public class CancelBookingTests
{
    private static async Task<(EventPassDbContext Db, BookingService Service, int EventId, int BookingId)> SetupAsync(int quantity = 3)
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        var db = new EventPassDbContext(new DbContextOptionsBuilder<EventPassDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();

        var venue = new Venue { Name = "H", Capacity = 100 };
        db.Venues.Add(venue);
        await db.SaveChangesAsync();
        var ev = new Event { Title = "E", TotalTickets = 10, AvailableSeats = 10, VenueId = venue.Id };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var service = new BookingService(db);
        var booked = await service.BookTicketsAsync(new BookTicketRequest(ev.Id, quantity));
        return (db, service, ev.Id, booked.Result!.BookingId);
    }

    [Fact]
    public async Task Cancel_ShouldCancelValidTickets_ReturnSeats_AndWriteHistory()
    {
        var (db, service, eventId, bookingId) = await SetupAsync(3);

        var result = await service.CancelBookingAsync(bookingId);

        Assert.True(result.Success);
        Assert.Equal("Cancelled", result.Result!.Status);
        Assert.All(result.Result.Tickets, t => Assert.Equal("Cancelled", t.Status));
        Assert.Equal(10, (await db.Events.AsNoTracking().SingleAsync(e => e.Id == eventId)).AvailableSeats);

        var last = result.Result.History.Last();
        Assert.Equal("Confirmed", last.OldStatus);
        Assert.Equal("Cancelled", last.NewStatus);
    }

    [Fact]
    public async Task Cancel_ShouldFail_WhenAlreadyCancelled_AndNotReturnSeatsTwice()
    {
        var (db, service, eventId, bookingId) = await SetupAsync(2);
        await service.CancelBookingAsync(bookingId);

        var second = await service.CancelBookingAsync(bookingId);

        Assert.Equal("booking_already_cancelled", second.ErrorCode);
        Assert.Equal(10, (await db.Events.AsNoTracking().SingleAsync(e => e.Id == eventId)).AvailableSeats);
    }

    [Fact]
    public async Task Cancel_ShouldFail_WhenBookingNotFound()
    {
        var (_, service, _, _) = await SetupAsync();
        var result = await service.CancelBookingAsync(999);
        Assert.Equal("booking_not_found", result.ErrorCode);
    }

    [Fact]
    public async Task Cancel_ShouldKeepScannedTickets_AndReturnSeatsOnlyForValidOnes()
    {
        var (db, service, eventId, bookingId) = await SetupAsync(3);
        var first = await db.QrTickets.AsNoTracking().OrderBy(t => t.Id).FirstAsync(t => t.BookingId == bookingId);
        await service.CheckTicketAsync(first.Token); // один квиток вже використано

        var result = await service.CancelBookingAsync(bookingId);

        Assert.True(result.Success);
        Assert.Equal(1, result.Result!.Tickets.Count(t => t.Status == "Scanned"));
        Assert.Equal(2, result.Result.Tickets.Count(t => t.Status == "Cancelled"));
        // було 10 - 3 = 7, повертаємо лише 2 скасовані
        Assert.Equal(9, (await db.Events.AsNoTracking().SingleAsync(e => e.Id == eventId)).AvailableSeats);
    }

    [Fact]
    public async Task Cancel_ShouldFailAndRollback_WhenAllTicketsAlreadyScanned()
    {
        var (db, service, eventId, bookingId) = await SetupAsync(1);
        var ticket = await db.QrTickets.AsNoTracking().SingleAsync(t => t.BookingId == bookingId);
        await service.CheckTicketAsync(ticket.Token);

        var result = await service.CancelBookingAsync(bookingId);

        Assert.Equal("booking_not_cancellable", result.ErrorCode);
        var booking = await db.Bookings.AsNoTracking().SingleAsync(b => b.Id == bookingId);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);   // відкат статусу
        Assert.Equal(9, (await db.Events.AsNoTracking().SingleAsync(e => e.Id == eventId)).AvailableSeats);
    }

    [Fact]
    public async Task CheckTicket_ShouldFail_ForTicketOfCancelledBooking()
    {
        var (db, service, _, bookingId) = await SetupAsync(1);
        var ticket = await db.QrTickets.AsNoTracking().SingleAsync(t => t.BookingId == bookingId);
        await service.CancelBookingAsync(bookingId);

        var result = await service.CheckTicketAsync(ticket.Token);

        Assert.Equal("ticket_cancelled", result.ErrorCode);
    }
}
