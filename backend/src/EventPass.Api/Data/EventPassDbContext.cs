using EventPass.Api.Entity;
using Microsoft.EntityFrameworkCore;

namespace EventPass.Api.Data;

public sealed class EventPassDbContext : DbContext
{
    public EventPassDbContext(DbContextOptions<EventPassDbContext> options) : base(options) { }
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<QrTicket> QrTickets => Set<QrTicket>();
    public DbSet<BookingHistory> BookingHistories => Set<BookingHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<QrTicket>()
            .HasIndex(t => t.Token)
            .IsUnique();
    }
}