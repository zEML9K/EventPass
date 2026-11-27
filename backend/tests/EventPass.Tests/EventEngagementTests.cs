using EventPass.Api.Data;
using EventPass.Api.DTO;
using EventPass.Api.Entity;
using EventPass.Api.Repositories;
using EventPass.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventPass.Tests;

public class EventEngagementTests
{
    private static async Task<(EventPassDbContext Db, EventEngagementFacade Facade, int EventId)> SetupAsync()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        var db = new EventPassDbContext(
            new DbContextOptionsBuilder<EventPassDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();

        var venue = new Venue { Name = "Hall", Address = "Addr", Capacity = 100 };
        db.Venues.Add(venue);
        await db.SaveChangesAsync();

        var ev = new Event
        {
            Title = "Concert",
            Description = "Desc",
            Date = DateTime.UtcNow.AddDays(3),
            TotalTickets = 50,
            AvailableSeats = 50,
            VenueId = venue.Id,
            Category = "Концерт"
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var facade = new EventEngagementFacade(new EfEventRepository(db));
        return (db, facade, ev.Id);
    }

    [Fact]
    public async Task ReportCommentTrimmed()
    {
        var (db, facade, eventId) = await SetupAsync();
        var paddedComment = "  " + new string('А', 500) + "  ";

        var result = await facade.ReportEventAsync(
            eventId,
            new CreateReportRequest("Інше", paddedComment, "client-1"));

        Assert.True(result.Success);
        Assert.Null(result.ErrorCode);

        var saved = await db.EventReports.AsNoTracking().SingleAsync();
        Assert.Equal(500, saved.Comment.Length);
    }

    [Fact]
    public async Task ReportCategoryTooLong()
    {
        var (db, facade, eventId) = await SetupAsync();
        var longCategory = new string('К', 101);

        var result = await facade.ReportEventAsync(
            eventId,
            new CreateReportRequest(longCategory, "Опис", "client-1"));

        Assert.False(result.Success);
        Assert.Equal("invalid_report_category", result.ErrorCode);
        Assert.Empty(db.EventReports);
    }

    [Fact]
    public async Task ReportDuplicateFails()
    {
        var (db, facade, eventId) = await SetupAsync();

        var first = await facade.ReportEventAsync(eventId, new CreateReportRequest("Інше", "Перша", "client-1"));
        var second = await facade.ReportEventAsync(eventId, new CreateReportRequest("Інше", "Друга", "client-1"));

        Assert.True(first.Success);
        Assert.False(second.Success);
        Assert.Equal("already_reported", second.ErrorCode);
        Assert.Single(db.EventReports);
    }

    [Fact]
    public async Task RateEventInvalidRatingFails()
    {
        var (_, facade, eventId) = await SetupAsync();

        var result = await facade.RateEventAsync(eventId, new RateEventRequest(0, "Погано", "client-1"));

        Assert.False(result.Success);
        Assert.Equal("invalid_rating", result.ErrorCode);
    }
}