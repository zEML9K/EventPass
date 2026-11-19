using EventPass.Api.DTO;
using EventPass.Api.Services;

namespace EventPass.Api.Endpoints;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var bookingsGroup = app.MapGroup("/api/bookings");

        // Історія бронювань
        bookingsGroup.MapGet("", async (BookingFacade facade) =>
            Results.Ok(await facade.GetBookingsAsync()));

        bookingsGroup.MapGet("/{id:int}", async Task<IResult> (int id, BookingFacade facade) =>
        {
            var booking = await facade.GetBookingByIdAsync(id);
            return booking == null ? ApiResults.FromError("booking_not_found") : Results.Ok(booking);
        });

        // UC-05: Скасувати замовлення
        bookingsGroup.MapPost("/{id:int}/cancel", async Task<IResult> (int id, BookingFacade facade) =>
        {
            var (success, errorCode, result) = await facade.CancelBookingAsync(id);
            return success ? Results.Ok(result) : ApiResults.FromError(errorCode);
        });

        // UC-03: Забронювати квиток
        bookingsGroup.MapPost("", async Task<IResult> (BookTicketRequest req, BookingFacade facade) =>
        {
            var (success, errorCode, result) = await facade.BookTicketsAsync(req);
            return success
                ? Results.Created($"/api/bookings/{result!.BookingId}", result)
                : ApiResults.FromError(errorCode);
        });

        // UC-04: Сканувати / перевірити квиток контролером
        app.MapPost("/api/tickets/check", async Task<IResult> (CheckTicketRequest req, BookingFacade facade) =>
        {
            var (success, errorCode, ticket) = await facade.CheckTicketAsync(req?.Token);
            return success ? Results.Ok(ticket) : ApiResults.FromError(errorCode);
        });

        return app;
    }
}