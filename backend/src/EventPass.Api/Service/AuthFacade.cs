using EventPass.Api.DTO;
using EventPass.Api.Entity;
using EventPass.Api.Repositories;
using EventPass.Api.Rules;

namespace EventPass.Api.Services;

/// <summary>
/// Фасад автентифікації та авторизації: реєстрація, вхід, перевірка ролі за токеном
/// і збирання активності акаунту (бронювання, відгуки, скарги) з фасадів підсистем.
/// </summary>
public sealed class AuthFacade
{
    private const string BearerPrefix = "Bearer ";

    private readonly IUserRepository _users;
    private readonly BookingFacade _bookings;
    private readonly EventEngagementFacade _engagement;

    public AuthFacade(IUserRepository users, BookingFacade bookings, EventEngagementFacade engagement)
    {
        _users = users;
        _bookings = bookings;
        _engagement = engagement;
    }

    public async Task<(bool Success, string? ErrorCode, AuthResponse? Result)> RegisterAsync(RegisterRequest? request)
    {
        var (isValid, username, errorCode) = AuthInputRule.Validate(request?.Username, request?.Password);
        if (!isValid)
            return (false, errorCode, null);

        if (!TryParseRole(request!.Role, out var role))
            return (false, "invalid_role", null);

        if (await _users.GetByUsernameAsync(username) != null)
            return (false, "username_taken", null);

        var token = PasswordHasher.NewToken();
        var user = new User
        {
            Username = username,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = role,
            ClientId = Guid.NewGuid().ToString("N"),
            SessionTokenHash = PasswordHasher.HashToken(token)
        };

        return await _users.AddAsync(user)
            ? (true, null, ToResponse(user, token))
            : (false, "username_taken", null);
    }

    public async Task<(bool Success, string? ErrorCode, AuthResponse? Result)> LoginAsync(LoginRequest? request)
    {
        var (isValid, username, errorCode) = AuthInputRule.Validate(request?.Username, request?.Password);
        if (!isValid)
            return (false, errorCode, null);

        if (!TryParseRole(request!.Role, out var role))
            return (false, "invalid_role", null);

        var user = await _users.GetByUsernameAsync(username);
        if (user == null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            return (false, "invalid_credentials", null);

        if (user.Role != role)
            return (false, "role_mismatch", null);

        var token = PasswordHasher.NewToken();
        await _users.SetSessionAsync(user, PasswordHasher.HashToken(token));

        return (true, null, ToResponse(user, token));
    }

    /// <summary>Визначає користувача за Bearer-токеном і перевіряє, що його роль дозволена.</summary>
    public async Task<(bool Success, string? ErrorCode, User? User)> AuthorizeAsync(
        string? authorizationHeader, params UserRole[] allowedRoles)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !authorizationHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            return (false, "unauthorized", null);

        var token = authorizationHeader[BearerPrefix.Length..].Trim();
        if (token.Length == 0)
            return (false, "unauthorized", null);

        var user = await _users.GetBySessionTokenHashAsync(PasswordHasher.HashToken(token));
        if (user == null)
            return (false, "unauthorized", null);

        if (allowedRoles.Length > 0 && !allowedRoles.Contains(user.Role))
            return (false, "forbidden_role", null);

        return (true, null, user);
    }

    public async Task<UserActivityDto> GetActivityAsync(User user)
    {
        var bookings = await _bookings.GetBookingsAsync(user.Id);
        var reviews = await _engagement.GetUserReviewsAsync(user.ClientId);
        var reports = await _engagement.GetUserReportsAsync(user.ClientId);

        return new UserActivityDto(
            new UserProfileDto(user.Id, user.Username, user.Role.ToString(), user.ClientId, user.CreatedAt),
            bookings, reviews, reports);
    }

    private static bool TryParseRole(string? raw, out UserRole role) =>
        Enum.TryParse(raw?.Trim(), ignoreCase: true, out role) && Enum.IsDefined(role);

    private static AuthResponse ToResponse(User u, string token) =>
        new(u.Id, u.Username, u.Role.ToString(), u.ClientId, token);
}