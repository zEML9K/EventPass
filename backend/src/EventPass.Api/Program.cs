using EventPass.Api.DTO;
using EventPass.Api.Data;
using EventPass.Api.Entity;
using EventPass.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<EventPassDbContext>(options =>
    options.UseSqlite("Data Source=eventpass.db"));
builder.Services.AddScoped<BookingService>();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EventPassDbContext>();
    db.Database.EnsureCreated();
    
    if (!db.Venues.Any())
    {
        var venue = new Venue { Name = "Головний Зал", Address = "м. Суми, вул. Університетська 1", Capacity = 100 };
        db.Venues.Add(venue);
        db.SaveChanges();

        db.Events.Add(new Event
        {
            Title = "IT Conference 2026",
            Description = "Конференція з розробки ПЗ",
            Date = DateTime.UtcNow.AddDays(7),
            TotalTickets = 50,
            AvailableSeats = 50,
            VenueId = venue.Id
        });
        db.SaveChanges();
    }
}

// UC-01: Отримати список подій
app.MapGet("/api/events", async (EventPassDbContext db) =>
{
    var events = await db.Events
        .Select(e => new EventDto(e.Id, e.Title, e.Description, e.Date, e.TotalTickets, e.AvailableSeats))
        .ToListAsync();
    return Results.Ok(events);
});

// UC-03: Забронювати квиток
app.MapPost("/api/bookings", async (BookTicketRequest req, BookingService service) =>
{
    var (success, errorCode, result) = await service.BookTicketsAsync(req);
    if (!success)
    {
        return errorCode switch
        {
            "insufficient_seats" => Results.Conflict(new ApiError(errorCode, "Недостатньо вільних місць.")),
            "event_not_found" => Results.NotFound(new ApiError(errorCode, "Подію не знайдено.")),
            _ => Results.BadRequest(new ApiError(errorCode ?? "invalid_request", "Некоректний запит."))
        };
    }
    return Results.Created($"/api/bookings/{result!.BookingId}", result);
});

// UC-04: Сканувати/перевірити квиток контролером
app.MapPost("/api/tickets/check", async (CheckTicketRequest req, BookingService service) =>
{
    var (success, errorCode, ticket) = await service.CheckTicketAsync(req.Token);
    if (!success)
    {
        return errorCode switch
        {
            "ticket_not_found" => Results.NotFound(new ApiError(errorCode, "Квиток не знайдено.")),
            "ticket_already_used" => Results.Conflict(new ApiError(errorCode, "Квиток вже використано.")),
            "ticket_cancelled" => Results.Conflict(new ApiError(errorCode, "Квиток скасовано.")),
            _ => Results.BadRequest(new ApiError(errorCode ?? "invalid_request", "Некоректний запит."))
        };
    }
    return Results.Ok(ticket);
});

app.Run();

public partial class Program { }