using EventPass.Api;
using EventPass.Api.Data;
using EventPass.Api.DTO;
using EventPass.Api.Repositories;
using EventPass.Api.Services;
using Microsoft.EntityFrameworkCore;

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
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<EventEngagementFacade>();
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
}

var eventsGroup = app.MapGroup("/api/events");
eventsGroup.MapGet("", async (string? category, EventService service) =>
    Results.Ok(await service.GetAllAsync(category)));

eventsGroup.MapGet("/{id:int}", async Task<IResult> (int id, EventService service) =>
{
    var (success, errorCode, result) = await service.GetByIdAsync(id);
    return success ? Results.Ok(result) : ApiResults.FromError(errorCode);
});

eventsGroup.MapPost("", async Task<IResult> (CreateEventRequest req, EventService service) =>
{
    var (success, errorCode, result) = await service.CreateEventAsync(req);
    return success
        ? Results.Created($"/api/events/{result!.Id}", result)
        : ApiResults.FromError(errorCode);
});

eventsGroup.MapGet("/{id:int}/reviews", async (int id, EventEngagementFacade facade) =>
    Results.Ok(await facade.GetReviewsAsync(id)));

eventsGroup.MapPost("/{id:int}/reviews", async Task<IResult> (
    int id, RateEventRequest req, HttpContext http, EventEngagementFacade facade) =>
{
    var ip = http.Connection.RemoteIpAddress?.ToString();
    var (success, errorCode, result) = await facade.RateEventAsync(id, req, ip);
    return success ? Results.Ok(result) : ApiResults.FromError(errorCode);
});

eventsGroup.MapPost("/{id:int}/reports", async Task<IResult> (
    int id, CreateReportRequest req, HttpContext http, EventEngagementFacade facade) =>
{
    var ip = http.Connection.RemoteIpAddress?.ToString();
    var (success, errorCode) = await facade.ReportEventAsync(id, req, ip);
    return success
        ? Results.Ok(new { message = "Скаргу успішно надіслано." })
        : ApiResults.FromError(errorCode);
});

app.MapGet("/api/venues", async (EventService service) =>
    Results.Ok(await service.GetVenuesAsync()));

var bookingsGroup = app.MapGroup("/api/bookings");
bookingsGroup.MapGet("", async (BookingService service) =>
    Results.Ok(await service.GetBookingsAsync()));

bookingsGroup.MapGet("/{id:int}", async Task<IResult> (int id, BookingService service) =>
{
    var booking = (await service.GetBookingsAsync(id)).FirstOrDefault();
    return booking == null ? ApiResults.FromError("booking_not_found") : Results.Ok(booking);
});

bookingsGroup.MapPost("/{id:int}/cancel", async Task<IResult> (int id, BookingService service) =>
{
    var (success, errorCode, result) = await service.CancelBookingAsync(id);
    return success ? Results.Ok(result) : ApiResults.FromError(errorCode);
});

bookingsGroup.MapPost("", async Task<IResult> (BookTicketRequest req, BookingService service) =>
{
    var (success, errorCode, result) = await service.BookTicketsAsync(req);
    return success
        ? Results.Created($"/api/bookings/{result!.BookingId}", result)
        : ApiResults.FromError(errorCode);
});

app.MapPost("/api/tickets/check", async Task<IResult> (CheckTicketRequest req, BookingService service) =>
{
    var (success, errorCode, ticket) = await service.CheckTicketAsync(req?.Token);
    return success ? Results.Ok(ticket) : ApiResults.FromError(errorCode);
});

app.Run();

public partial class Program { }