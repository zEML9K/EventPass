using EventPass.Api.Entity;
using EventPass.Api.Services;

namespace EventPass.Api.Endpoints;

public static class AuthorizationExtensions
{
    private const string UserItemKey = "CurrentUser";

    /// <summary>Вимагає токен і одну з дозволених ролей. Без ролей достатньо будь-якого автентифікованого користувача.</summary>
    public static RouteHandlerBuilder RequireRole(this RouteHandlerBuilder builder, params UserRole[] roles) =>
        builder.AddEndpointFilter(async (ctx, next) =>
        {
            var http = ctx.HttpContext;
            var auth = http.RequestServices.GetRequiredService<AuthFacade>();

            var (success, errorCode, user) =
                await auth.AuthorizeAsync(http.Request.Headers.Authorization.ToString(), roles);

            if (!success)
                return ApiResults.FromError(errorCode);

            http.Items[UserItemKey] = user;
            return await next(ctx);
        });

    public static User GetCurrentUser(this HttpContext http) => (User)http.Items[UserItemKey]!;
}