using EventPass.Api;
using EventPass.Api.DTO;
using EventPass.Api.Data;
using EventPass.Api.Entity;
using EventPass.Api.Services;
using Microsoft.EntityFrameworkCore;
using EventPass.Api.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not set.");

builder.Services.AddDbContext<EventPassDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));

builder.Services.AddSingleton<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IEventRepository, EfEventRepository>();
builder.Services.AddScoped<IBookingRepository, EfBookingRepository>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<EventService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EventPassDbContext>();
    
    db.Database.Migrate();

    if (!db.Venues.Any())
    {
        var venue = new Venue 
        { 
            Name = "Артпростір «Вежа»", 
            Address = "КПІ Корпус №1, Берестейський проспект, 37", 
            Capacity = 25 
        };
        db.Venues.Add(venue);
        db.SaveChanges();

        db.Events.Add(new Event
        {
            Title = "Програма Кіноекзорцизм",
            Description = "Перегляд психологічного трилера «Мати Іоанна від янголів»",
            Date = DateTime.UtcNow.AddDays(7),
            TotalTickets = 25,
            AvailableSeats = 25,
            VenueId = venue.Id,
            Category = "Кіно"
        });
        db.SaveChanges();
    }
}

// UC-01: Список подій
app.MapGet("/api/events", async (string? category, EventService service) =>
    Results.Ok(await service.GetAllAsync(category)));

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
    var booking = (await service.GetBookingsAsync(id)).FirstOrDefault();
    return booking == null ? ApiResults.FromError("booking_not_found") : Results.Ok(booking);
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
