using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EventPass.Api.Data;

public sealed class EventPassDbContextFactory : IDesignTimeDbContextFactory<EventPassDbContext>
{
    public static string GetDefaultSqliteConnectionString()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EventPass");
        Directory.CreateDirectory(dir);
        var dbPath = Path.Combine(dir, "eventpass.db");
        return $"Data Source={dbPath}";
    }
    
    public EventPassDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EventPassDbContext>();
        optionsBuilder.UseSqlite(GetDefaultSqliteConnectionString());
        return new EventPassDbContext(optionsBuilder.Options);
    }
}