using EventPass.Api.DTO;
using EventPass.Api.Services;

namespace EventPass.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async Task<IResult> (RegisterRequest req, AuthFacade facade) =>
        {
            var (success, errorCode, result) = await facade.RegisterAsync(req);
            return success
                ? Results.Created("/api/auth/me", result)
                : ApiResults.FromError(errorCode);
        });

        group.MapPost("/login", async Task<IResult> (LoginRequest req, AuthFacade facade) =>
        {
            var (success, errorCode, result) = await facade.LoginAsync(req);
            return success ? Results.Ok(result) : ApiResults.FromError(errorCode);
        });

        // Профіль і активність поточного користувача
        group.MapGet("/me", async Task<IResult> (HttpContext http, AuthFacade facade) =>
            Results.Ok(await facade.GetActivityAsync(http.GetCurrentUser())))
            .RequireRole();

        return app;
    }
}