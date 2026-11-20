using EventPass.Api.Data;
using EventPass.Api.DTO;
using EventPass.Api.Entity;
using EventPass.Api.Repositories;
using EventPass.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventPass.Tests;

public class AuthTests
{
    private static (EventPassDbContext Db, AuthFacade Auth) Setup()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        var db = new EventPassDbContext(new DbContextOptionsBuilder<EventPassDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();

        var auth = new AuthFacade(
            new EfUserRepository(db),
            new BookingFacade(new EfBookingRepository(db)),
            new EventEngagementFacade(new EfEventRepository(db)));
        return (db, auth);
    }

    private static Task<(bool Success, string? ErrorCode, AuthResponse? Result)> Register(
        AuthFacade a, string role = "Visitor", string user = "olena") =>
        a.RegisterAsync(new RegisterRequest(user, "secret1", role));

    [Fact]
    public void PasswordHasher_ShouldVerifyCorrectAndRejectWrongPassword()
    {
        var hash = PasswordHasher.Hash("secret1");

        Assert.DoesNotContain("secret1", hash);
        Assert.True(PasswordHasher.Verify("secret1", hash));
        Assert.False(PasswordHasher.Verify("other", hash));
    }

    [Fact]
    public async Task Register_ShouldStoreHashedPassword_AndRejectDuplicateUsername()
    {
        var (db, auth) = Setup();

        var first = await Register(auth);
        var second = await Register(auth);

        Assert.True(first.Success);
        Assert.NotEqual("secret1", (await db.Users.SingleAsync()).PasswordHash);
        Assert.Equal("username_taken", second.ErrorCode);
    }

    [Theory]
    [InlineData("ab", "secret1", "Visitor", "invalid_username")]
    [InlineData("olena", "123", "Visitor", "invalid_password")]
    [InlineData("olena", "secret1", "Admin", "invalid_role")]
    public async Task Register_ShouldFail_OnInvalidInput(string u, string p, string r, string code)
    {
        var (_, auth) = Setup();
        Assert.Equal(code, (await auth.RegisterAsync(new RegisterRequest(u, p, r))).ErrorCode);
    }

    [Fact]
    public async Task Login_ShouldSucceed_AndFailOnWrongPasswordOrRole()
    {
        var (_, auth) = Setup();
        await Register(auth, "Organizer");

        Assert.True((await auth.LoginAsync(new LoginRequest("olena", "secret1", "Organizer"))).Success);
        Assert.Equal("invalid_credentials",
            (await auth.LoginAsync(new LoginRequest("olena", "wrong-pass", "Organizer"))).ErrorCode);
        Assert.Equal("role_mismatch",
            (await auth.LoginAsync(new LoginRequest("olena", "secret1", "Visitor"))).ErrorCode);
    }

    [Fact]
    public async Task Login_ShouldInvalidateOldToken()
    {
        var (_, auth) = Setup();
        var old = (await Register(auth)).Result!.Token;

        await auth.LoginAsync(new LoginRequest("olena", "secret1", "Visitor"));

        Assert.Equal("unauthorized", (await auth.AuthorizeAsync($"Bearer {old}")).ErrorCode);
    }

    [Theory]
    [InlineData("Visitor", "Organizer", "forbidden_role")]
    [InlineData("Organizer", "Controller", "forbidden_role")]
    [InlineData("Controller", "Organizer", "forbidden_role")]
    [InlineData("Organizer", "Organizer", null)]
    [InlineData("Controller", "Controller", null)]
    public async Task Authorize_ShouldEnforceRole(string actual, string required, string? expectedError)
    {
        var (_, auth) = Setup();
        var token = (await Register(auth, actual)).Result!.Token;

        var result = await auth.AuthorizeAsync($"Bearer {token}", Enum.Parse<UserRole>(required));

        Assert.Equal(expectedError, result.ErrorCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Bearer bad-token")]
    [InlineData("Basic abc")]
    public async Task Authorize_ShouldReturnUnauthorized_ForMissingOrInvalidToken(string? header)
    {
        var (_, auth) = Setup();
        Assert.Equal("unauthorized", (await auth.AuthorizeAsync(header, UserRole.Visitor)).ErrorCode);
    }

    [Fact]
    public async Task Activity_ShouldContainOnlyOwnBookingsReviewsAndReports()
    {
        var (db, auth) = Setup();
        var venue = new Venue { Name = "H", Address = "A", Capacity = 100 };
        db.Venues.Add(venue);
        await db.SaveChangesAsync();
        var ev = new Event { Title = "E", Description = "D", TotalTickets = 20, AvailableSeats = 20, VenueId = venue.Id };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var alice = (await Register(auth, user: "alice")).Result!;
        var bob = (await Register(auth, user: "bob")).Result!;

        var bookings = new BookingFacade(new EfBookingRepository(db));
        var engagement = new EventEngagementFacade(new EfEventRepository(db));
        await bookings.BookTicketsAsync(new BookTicketRequest(ev.Id, 2), alice.UserId);
        await bookings.BookTicketsAsync(new BookTicketRequest(ev.Id, 1), bob.UserId);
        await engagement.RateEventAsync(ev.Id, new RateEventRequest(5, "ok", alice.ClientId));
        await engagement.RateEventAsync(ev.Id, new RateEventRequest(1, "bad", bob.ClientId));
        await engagement.ReportEventAsync(ev.Id, new CreateReportRequest("Інше", "x", alice.ClientId));

        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Username == "alice");
        var activity = await auth.GetActivityAsync(user);

        Assert.Equal(2, Assert.Single(activity.Bookings).Quantity);
        Assert.Equal(5, Assert.Single(activity.Reviews).Rating);
        Assert.Single(activity.Reports);
    }

    [Fact]
    public async Task Cancel_ShouldNotAllowForeignBooking()
    {
        var (db, auth) = Setup();
        var venue = new Venue { Name = "H", Address = "A", Capacity = 100 };
        db.Venues.Add(venue);
        await db.SaveChangesAsync();
        var ev = new Event { Title = "E", Description = "D", TotalTickets = 5, AvailableSeats = 5, VenueId = venue.Id };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var owner = (await Register(auth, user: "owner")).Result!;
        var stranger = (await Register(auth, user: "stranger")).Result!;
        var bookings = new BookingFacade(new EfBookingRepository(db));
        var booked = await bookings.BookTicketsAsync(new BookTicketRequest(ev.Id, 2), owner.UserId);

        var result = await bookings.CancelBookingAsync(booked.Result!.BookingId, stranger.UserId);

        Assert.Equal("booking_not_found", result.ErrorCode);
        Assert.Equal(3, (await db.Events.AsNoTracking().SingleAsync()).AvailableSeats);
    }
}