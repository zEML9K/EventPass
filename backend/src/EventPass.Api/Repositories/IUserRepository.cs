using EventPass.Api.Entity;

namespace EventPass.Api.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetBySessionTokenHashAsync(string tokenHash);
    Task<bool> AddAsync(User user);
    Task SetSessionAsync(User user, string tokenHash);
}