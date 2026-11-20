using EventPass.Api.DTO;
using EventPass.Api.Entity;
using EventPass.Api.Services;

namespace EventPass.Api.Endpoints;

public static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var eventsGroup = app.MapGroup("/api/events");

        // UC-01: Список подій
        eventsGroup.MapGet("", async (string? category, EventCatalogFacade facade) =>
            Results.Ok(await facade.GetEventsAsync(category)));

        // UC-01: Одна подія
        eventsGroup.MapGet("/{id:int}", async Task<IResult> (int id, EventCatalogFacade facade) =>
        {
            var (success, errorCode, result) = await facade.GetEventByIdAsync(id);
            return success ? Results.Ok(result) : ApiResults.FromError(errorCode);
        });

        // UC-02: Створити подію (тільки організатор)
        eventsGroup.MapPost("", async Task<IResult> (CreateEventRequest req, EventCatalogFacade facade) =>
        {
            var (success, errorCode, result) = await facade.CreateEventAsync(req);
            return success
                ? Results.Created($"/api/events/{result!.Id}", result)
                : ApiResults.FromError(errorCode);
        }).RequireRole(UserRole.Organizer);

        // Оцінки та відгуки про подію
        eventsGroup.MapGet("/{id:int}/reviews", async (int id, EventEngagementFacade facade) =>
            Results.Ok(await facade.GetReviewsAsync(id)));

        eventsGroup.MapPost("/{id:int}/reviews", async Task<IResult> (
            int id, RateEventRequest req, HttpContext http, EventEngagementFacade facade) =>
        {
            var ip = http.Connection.RemoteIpAddress?.ToString();
            var secured = req with { ClientId = http.GetCurrentUser().ClientId };
            var (success, errorCode, result) = await facade.RateEventAsync(id, secured, ip);
            return success ? Results.Ok(result) : ApiResults.FromError(errorCode);
        }).RequireRole(UserRole.Visitor);

        // Скарга / репорт на подію
        eventsGroup.MapPost("/{id:int}/reports", async Task<IResult> (
            int id, CreateReportRequest req, HttpContext http, EventEngagementFacade facade) =>
        {
            var ip = http.Connection.RemoteIpAddress?.ToString();
            var secured = req with { ClientId = http.GetCurrentUser().ClientId };
            var (success, errorCode) = await facade.ReportEventAsync(id, secured, ip);
            return success
                ? Results.Ok(new { message = "Скаргу успішно надіслано." })
                : ApiResults.FromError(errorCode);
        }).RequireRole(UserRole.Visitor);

        // Локації
        app.MapGet("/api/venues", async (EventCatalogFacade facade) =>
            Results.Ok(await facade.GetVenuesAsync()));

        return app;
    }
}