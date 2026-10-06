using AeroLink.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Data;

/// <summary>
/// Database context for AeroLink's flights, bags, and baggage exceptions.
/// Employee data stays in the HR system and is read through its API.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>Creates the context using the options configured in Program.cs or a test.</summary>
    /// <param name="options">Database provider and connection options.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>Flights stored in the local database.</summary>
    public DbSet<Flight> Flights => Set<Flight>();

    /// <summary>Bags assigned to flights.</summary>
    public DbSet<Bag> Bags => Set<Bag>();

    /// <summary>Exceptions reported for bags or flights.</summary>
    public DbSet<BaggageException> BaggageExceptions => Set<BaggageException>();

    /// <summary>Configures keys, relationships, enum storage and seed data.</summary>
    /// <param name="modelBuilder">The EF Core model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Flight>(flight =>
        {
            flight.HasKey(flight => flight.FlightId);
            flight.Property(flight => flight.FlightId).ValueGeneratedNever();
            flight.Property(flight => flight.LoadingStatus).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Bag>(bag =>
        {
            bag.HasKey(bag => bag.BagId);
            bag.HasIndex(bag => bag.Tag).IsUnique();
            bag.Property(bag => bag.HandlingType).HasConversion<string>().HasMaxLength(30);
            bag.Property(bag => bag.Outcome).HasConversion<string>().HasMaxLength(30);
            bag.HasOne(bag => bag.Flight)
                .WithMany(flight => flight.Bags)
                .HasForeignKey(bag => bag.FlightId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BaggageException>(baggageException =>
        {
            baggageException.HasKey(baggageException => baggageException.ExceptionId);
            baggageException.Property(baggageException => baggageException.Category)
                .HasConversion<string>()
                .HasMaxLength(30);
            baggageException.Property(baggageException => baggageException.Status)
                .HasConversion<string>()
                .HasMaxLength(30);
            baggageException.HasIndex(baggageException => new
            {
                baggageException.FlightId,
                baggageException.Status
            });
            baggageException.HasOne(baggageException => baggageException.Bag)
                .WithMany(bag => bag.Exceptions)
                .HasForeignKey(baggageException => baggageException.BagId)
                .OnDelete(DeleteBehavior.Restrict);
            baggageException.HasOne(baggageException => baggageException.Flight)
                .WithMany(flight => flight.Exceptions)
                .HasForeignKey(baggageException => baggageException.FlightId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        ManifestSeedData.Apply(modelBuilder);
    }
}
