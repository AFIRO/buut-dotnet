using Microsoft.EntityFrameworkCore;
using Rise.Domain.Bookings;
using Rise.Domain.Notifications;
using Rise.Domain.Users;

namespace Rise.Persistence;

/// <inheritdoc />
public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Boat> Boats => Set<Boat>();
    public DbSet<Battery> Batteries => Set<Battery>();
    public DbSet<Notification> Notifications => Set<Notification>();

    public ApplicationDbContext(DbContextOptions options) : base(options)
    {

    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // All columns in the database have a maxlength of 4000.
        // in NVARACHAR 4000 is the maximum length that can be indexed by a database.
        // Some columns need more length, but these can be set on the configuration level for that Entity in particular.
        configurationBuilder.Properties<string>().HaveMaxLength(4_000);
        // All decimals columns should have 2 digits after the comma
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Applying all types of IEntityTypeConfiguration in the Persistence project.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

          // Configure many-to-many relationship between User and Role
        modelBuilder.Entity<User>()
            .HasMany(u => u.Roles)
            .WithMany(r => r.Users)
            .UsingEntity<Dictionary<string, object>>(
                "UserRole", // Name of the join table
                userRole => userRole
                    .HasOne<Role>()
                    .WithMany()
                    .HasForeignKey("RoleId")
                    .OnDelete(DeleteBehavior.Cascade),
                userRole => userRole
                    .HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade)
            );
    }

}

