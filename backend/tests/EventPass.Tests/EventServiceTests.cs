using EventPass.Api.Data;
using EventPass.Api.DTO;
using EventPass.Api.Entity;
using EventPass.Api.Repositories;
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

    private static EventCatalogFacade NewService(EventPassDbContext db) =>
        new(new EfEventRepository(db));

    private static CreateEventRequest Req(int venueId, int tickets = 50, string title = "Conf", DateTime? date = null, string category = "Кіно") =>
        new(title, "Desc", date ?? DateTime.UtcNow.AddDays(5), tickets, venueId, category);

    [Fact]
    public async Task CreateEvent_ShouldSucceed_AndSetAvailableSeats()
    {
        var (db, venue) = await SetupAsync();
        var result = await NewService(db).CreateEventAsync(Req(venue.Id, 40));

        Assert.True(result.Success);
        Assert.Equal(40, result.Result!.AvailableSeats);
        Assert.Equal("Hall", result.Result.VenueName);
    }

    [Fact]
    public async Task CreateEvent_ShouldFail_WhenTicketsExceedCapacity()
    {
        var (db, venue) = await SetupAsync(capacity: 10);
        var result = await NewService(db).CreateEventAsync(Req(venue.Id, 11));

        Assert.False(result.Success);
        Assert.Equal("tickets_exceed_capacity", result.ErrorCode);
        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task CreateEvent_ShouldFail_WhenVenueNotFound()
    {
        var (db, _) = await SetupAsync();
        var result = await NewService(db).CreateEventAsync(Req(999));
        Assert.Equal("venue_not_found", result.ErrorCode);
    }

    [Theory]
    [InlineData("", 10, 5, "invalid_title")]
    [InlineData("T", 0, 5, "invalid_total_tickets")]
    [InlineData("T", 10, -1, "invalid_date")]
    public async Task CreateEvent_ShouldFail_OnInvalidData(string title, int tickets, int daysFromNow, string code)
    {
        var (db, venue) = await SetupAsync();
        var result = await NewService(db)
            .CreateEventAsync(Req(venue.Id, tickets, title, DateTime.UtcNow.AddDays(daysFromNow)));

        Assert.Equal(code, result.ErrorCode);
        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_ForMissingEvent()
    {
        var (db, _) = await SetupAsync();
        var result = await NewService(db).GetEventByIdAsync(42);
        Assert.Equal("event_not_found", result.ErrorCode);
    }

    [Fact]
    public async Task CreateEvent_ShouldPersistCategory_AndFilterByCategory()
    {
        var (db, venue) = await SetupAsync();
        var service = NewService(db);

        await service.CreateEventAsync(new CreateEventRequest("Фільм 1", "Опис", DateTime.UtcNow.AddDays(2), 10, venue.Id, "Кіно"));
        await service.CreateEventAsync(new CreateEventRequest("Рок-вечір", "Опис", DateTime.UtcNow.AddDays(3), 10, venue.Id, "Концерт"));

        var cinemaOnly = await service.GetEventsAsync("Кіно");
        var emptyTheatre = await service.GetEventsAsync("Театр");

        Assert.Single(cinemaOnly);
        Assert.Equal("Кіно", cinemaOnly[0].Category);
        Assert.Empty(emptyTheatre);
    }

    [Fact]
    public async Task CreateEvent_ShouldFail_WhenCategoryExceeds50Chars()
    {
        var (db, venue) = await SetupAsync();
        var longCategory = new string('К', 51);

        var result = await NewService(db).CreateEventAsync(
            new CreateEventRequest("Подія", "Опис", DateTime.UtcNow.AddDays(2), 10, venue.Id, longCategory));

        Assert.False(result.Success);
        Assert.Equal("category_too_long", result.ErrorCode);
        Assert.Empty(db.Events);
    }
}