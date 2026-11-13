using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EventPass.Api.Data;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<EventPassDbContext>
{
    public EventPassDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var cs = config.GetConnectionString("DefaultConnection")!;
        var options = new DbContextOptionsBuilder<EventPassDbContext>()
            .UseMySql(cs, new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;
        return new EventPassDbContext(options);
    }
}