using EventPass.Api;
using EventPass.Api.Data;
using EventPass.Api.DTO;
using EventPass.Api.Entity;
using EventPass.Api.Repositories;
using EventPass.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var storageProvider = builder.Configuration["StorageProvider"] ?? "Sqlite";

if (storageProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<InMemoryStorage>();
    builder.Services.AddSingleton<IEventRepository, InMemoryEventRepository>();
    builder.Services.AddSingleton<IBookingRepository, InMemoryBookingRepository>();
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? EventPassDbContextFactory.GetDefaultSqliteConnectionString();

    builder.Services.AddDbContext<EventPassDbContext>(options =>
        options.UseSqlite(connectionString));

    builder.Services.AddScoped<IEventRepository, EfEventRepository>();
    builder.Services.AddScoped<IBookingRepository, EfBookingRepository>();
}

builder.Services.AddSingleton<IQrCodeService, QrCodeService>();

builder.Services.AddScoped(sp => new BookingService(
    sp.GetRequiredService<IBookingRepository>(),
    sp.GetRequiredService<IQrCodeService>()));

builder.Services.AddScoped(sp => new EventService(
    sp.GetRequiredService<IEventRepository>()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!storageProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<EventPassDbContext>();
    await db.Database.MigrateAsync();

    if (!await db.Venues.AnyAsync())
    {
        var venue = new Venue
        {
            Name = "Артпростір «Вежа»",
            Address = "КПІ Корпус №1, Берестейський проспект, 37",
            Capacity = 25
        };
        db.Venues.Add(venue);
        await db.SaveChangesAsync();

        db.Events.Add(new Event
        {
            Title = "Програма Кіноекзорцизм",
            Description = "Перегляд психологічного трилера «Мати Іоанна від янголів»",
            Category = "Кіно",
            Date = DateTime.UtcNow.AddDays(7),
            TotalTickets = 25,
            AvailableSeats = 25,
            VenueId = venue.Id
        });
        await db.SaveChangesAsync();
    }
}

// UC-01 + Для Lab5: список подій із пошуком за назвою/описом та фільтрацією за категорією
app.MapGet("/api/events", async (string? search, string? category, EventService service) =>
    Results.Ok(await service.GetAllAsync(search, category)));

// UC-01: Одна подія
app.MapGet("/api/events/{id:int}", async (int id, EventService service) =>
{
    var (success, errorCode, result) = await service.GetByIdAsync(id);
    return success ? Results.Ok(result) : ApiResults.FromError(errorCode);
});

// UC-02: Створити подію
app.MapPost("/api/events", async (CreateEventRequest req, EventService service) =>
{
    var (success, errorCode, result) = await service.CreateEventAsync(req);
    return success
        ? Results.Created($"/api/events/{result!.Id}", result)
        : ApiResults.FromError(errorCode);
});

// Локації (для випадаючого списку при створенні події)
app.MapGet("/api/venues", async (EventService service) =>
    Results.Ok(await service.GetVenuesAsync()));

// Історія бронювань
app.MapGet("/api/bookings", async (BookingService service) =>
    Results.Ok(await service.GetBookingsAsync()));

app.MapGet("/api/bookings/{id:int}", async (int id, BookingService service) =>
{
    var list = await service.GetBookingsAsync(id);
    return list.Count == 0 ? ApiResults.FromError("booking_not_found") : Results.Ok(list[0]);
});

// UC-05: Скасувати замовлення
app.MapPost("/api/bookings/{id:int}/cancel", async (int id, BookingService service) =>
{
    var (success, errorCode, result) = await service.CancelBookingAsync(id);
    return success ? Results.Ok(result) : ApiResults.FromError(errorCode);
});

// UC-03: Забронювати квиток
app.MapPost("/api/bookings", async (BookTicketRequest req, BookingService service) =>
{
    var (success, errorCode, result) = await service.BookTicketsAsync(req);
    return success
        ? Results.Created($"/api/bookings/{result!.BookingId}", result)
        : ApiResults.FromError(errorCode);
});

// UC-04: Сканувати/перевірити квиток контролером
app.MapPost("/api/tickets/check", async (CheckTicketRequest req, BookingService service) =>
{
    var (success, errorCode, ticket) = await service.CheckTicketAsync(req?.Token);
    return success ? Results.Ok(ticket) : ApiResults.FromError(errorCode);
});

app.Run();

public partial class Program { }