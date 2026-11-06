using EventPass.Api.Data;
using EventPass.Api.DTO;
using EventPass.Api.Entity;
using EventPass.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventPass.Tests;

public class EventServiceTests
{
    private static async Task<(EventPassDbContext Db, Venue Venue)> SetupAsync(int capacity = 100)
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        var db = new EventPassDbContext(new DbContextOptionsBuilder<EventPassDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();

        var venue = new Venue { Name = "Hall", Address = "Addr", Capacity = capacity };
        db.Venues.Add(venue);
        await db.SaveChangesAsync();
        return (db, venue);
    }

    private static CreateEventRequest Req(int venueId, int tickets = 50, string title = "Conf", DateTime? date = null) =>
        new(title, "Desc", date ?? DateTime.UtcNow.AddDays(5), tickets, venueId);

    [Fact]
    public async Task CreateEvent_ShouldSucceed_AndSetAvailableSeats()
    {
        var (db, venue) = await SetupAsync();
        var result = await new EventService(db).CreateEventAsync(Req(venue.Id, 40));

        Assert.True(result.Success);
        Assert.Equal(40, result.Result!.AvailableSeats);
        Assert.Equal("Hall", result.Result.VenueName);
    }

    [Fact]
    public async Task CreateEvent_ShouldFail_WhenTicketsExceedCapacity()
    {
        var (db, venue) = await SetupAsync(capacity: 10);
        var result = await new EventService(db).CreateEventAsync(Req(venue.Id, 11));

        Assert.False(result.Success);
        Assert.Equal("tickets_exceed_capacity", result.ErrorCode);
        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task CreateEvent_ShouldFail_WhenVenueNotFound()
    {
        var (db, _) = await SetupAsync();
        var result = await new EventService(db).CreateEventAsync(Req(999));
        Assert.Equal("venue_not_found", result.ErrorCode);
    }

    [Theory]
    [InlineData("", 10, 5, "invalid_title")]
    [InlineData("T", 0, 5, "invalid_total_tickets")]
    [InlineData("T", 10, -1, "invalid_date")]
    public async Task CreateEvent_ShouldFail_OnInvalidData(string title, int tickets, int daysFromNow, string code)
    {
        var (db, venue) = await SetupAsync();
        var result = await new EventService(db)
            .CreateEventAsync(Req(venue.Id, tickets, title, DateTime.UtcNow.AddDays(daysFromNow)));

        Assert.Equal(code, result.ErrorCode);
        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_ForMissingEvent()
    {
        var (db, _) = await SetupAsync();
        var result = await new EventService(db).GetByIdAsync(42);
        Assert.Equal("event_not_found", result.ErrorCode);
    }
}
