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
    public DbSet<EventReview> EventReviews => Set<EventReview>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Venue>(e =>
        {
            e.ToTable("Venues");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Address).IsRequired().HasMaxLength(300);
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.ToTable("Events");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description).IsRequired();
            e.HasOne(x => x.Venue).WithMany()
                .HasForeignKey(x => x.VenueId)
                .IsRequired().OnDelete(DeleteBehavior.Restrict);

            // Category event and filter category
            e.Property(x => x.Category).IsRequired().HasMaxLength(50).HasDefaultValue("");

            // Event ratings
            e.Property(x => x.AverageRating).HasDefaultValue(0.0);
            e.Property(x => x.ReviewsCount).HasDefaultValue(0);
        });

        modelBuilder.Entity<EventReview>(e =>
        {
            e.ToTable("EventReviews");
            e.HasKey(x => x.Id);
            e.Property(x => x.Rating).IsRequired();
            e.Property(x => x.Comment).IsRequired().HasMaxLength(300).HasDefaultValue("");
            e.HasOne(x => x.Event).WithMany()
                .HasForeignKey(x => x.EventId)
                .IsRequired().OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Booking>(e =>
        {
            e.ToTable("Bookings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Event).WithMany()
                .HasForeignKey(x => x.EventId)
                .IsRequired().OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<QrTicket>(e =>
        {
            e.ToTable("QrTickets");
            e.HasKey(x => x.Id);
            e.Property(x => x.Token).IsRequired().HasMaxLength(64);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.Token).IsUnique();
            e.HasOne(x => x.Booking).WithMany(b => b.Tickets)
                .HasForeignKey(x => x.BookingId)
                .IsRequired().OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BookingHistory>(e =>
        {
            e.ToTable("BookingHistories");
            e.HasKey(x => x.Id);
            e.Property(x => x.OldStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.NewStatus).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Booking).WithMany()
                .HasForeignKey(x => x.BookingId)
                .IsRequired().OnDelete(DeleteBehavior.Cascade);
        });
    }
}
