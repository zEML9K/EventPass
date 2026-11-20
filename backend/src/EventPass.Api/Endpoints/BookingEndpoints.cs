using EventPass.Api.DTO;
using EventPass.Api.Entity;
using EventPass.Api.Services;

namespace EventPass.Api.Endpoints;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var bookingsGroup = app.MapGroup("/api/bookings");

        // Історія власних бронювань
        bookingsGroup.MapGet("", async (HttpContext http, BookingFacade facade) =>
            Results.Ok(await facade.GetBookingsAsync(http.GetCurrentUser().Id)))
            .RequireRole(UserRole.Visitor);

        bookingsGroup.MapGet("/{id:int}", async Task<IResult> (int id, HttpContext http, BookingFacade facade) =>
        {
            var booking = await facade.GetBookingByIdAsync(id, http.GetCurrentUser().Id);
            return booking == null ? ApiResults.FromError("booking_not_found") : Results.Ok(booking);
        }).RequireRole(UserRole.Visitor);

        // UC-05: Скасувати власне замовлення
        bookingsGroup.MapPost("/{id:int}/cancel", async Task<IResult> (int id, HttpContext http, BookingFacade facade) =>
        {
            var (success, errorCode, result) = await facade.CancelBookingAsync(id, http.GetCurrentUser().Id);
            return success ? Results.Ok(result) : ApiResults.FromError(errorCode);
        }).RequireRole(UserRole.Visitor);

        // UC-03: Забронювати квиток
        bookingsGroup.MapPost("", async Task<IResult> (BookTicketRequest req, HttpContext http, BookingFacade facade) =>
        {
            var (success, errorCode, result) = await facade.BookTicketsAsync(req, http.GetCurrentUser().Id);
            return success
                ? Results.Created($"/api/bookings/{result!.BookingId}", result)
                : ApiResults.FromError(errorCode);
        }).RequireRole(UserRole.Visitor);

        // UC-04: Сканувати / перевірити квиток (тільки контролер)
        app.MapPost("/api/tickets/check", async Task<IResult> (CheckTicketRequest req, BookingFacade facade) =>
        {
            var (success, errorCode, ticket) = await facade.CheckTicketAsync(req?.Token);
            return success ? Results.Ok(ticket) : ApiResults.FromError(errorCode);
        }).RequireRole(UserRole.Controller);

        return app;
    }
}