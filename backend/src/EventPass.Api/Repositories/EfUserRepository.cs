using EventPass.Api.Data;
using EventPass.Api.Entity;
using Microsoft.EntityFrameworkCore;

namespace EventPass.Api.Repositories;

public sealed class EfUserRepository : IUserRepository
{
    private readonly EventPassDbContext _db;

    public EfUserRepository(EventPassDbContext db) => _db = db;

    public Task<User?> GetByUsernameAsync(string username) =>
        _db.Users.FirstOrDefaultAsync(u => u.Username == username);

    public Task<User?> GetBySessionTokenHashAsync(string tokenHash) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.SessionTokenHash == tokenHash);

    public async Task<bool> AddAsync(User user)
    {
        _db.Users.Add(user);
        try
        {
            await _db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            // Гонка за унікальний індекс Username
            _db.Entry(user).State = EntityState.Detached;
            return false;
        }
    }

    public async Task SetSessionAsync(User user, string tokenHash)
    {
        user.SessionTokenHash = tokenHash;
        await _db.SaveChangesAsync();
    }
}