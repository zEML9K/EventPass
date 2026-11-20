namespace EventPass.Api.Rules;

public static class AuthInputRule
{
    public static (bool IsValid, string Username, string? ErrorCode) Validate(string? username, string? password)
    {
        var name = (username ?? string.Empty).Trim();
        if (name.Length < 3 || name.Length > 50)
            return (false, string.Empty, "invalid_username");

        if (string.IsNullOrEmpty(password) || password.Length < 6 || password.Length > 100)
            return (false, name, "invalid_password");

        return (true, name, null);
    }
}