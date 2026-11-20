namespace EventPass.Api.Entity;

public sealed class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Visitor;

    public string ClientId { get; set; } = Guid.NewGuid().ToString("N");

    public string? SessionTokenHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}