namespace EventPass.Api.Rules;

public static class ClientIdentityNormalizer
{
    public const int MaxLength = 64;

    public static string Normalize(string? rawValue)
    {
        var normalized = (rawValue ?? string.Empty).Trim();
        return normalized.Length > MaxLength
            ? normalized[..MaxLength]
            : normalized;
    }
}