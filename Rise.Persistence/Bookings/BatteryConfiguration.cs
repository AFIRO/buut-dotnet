using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.Bookings;
using Rise.Domain.Users;

namespace Rise.Persistence.Bookings;

internal class BatteryConfiguration : EntityConfiguration<Battery>
{
    /// <summary>
    /// Configures the <see cref="Battery"/> entity using the specified <see cref="EntityTypeBuilder"/>.
    /// </summary>
    /// <param name="builder">The builder used to configure the <see cref="Battery"/> entity.</param>
    public override void Configure(EntityTypeBuilder<Battery> builder)
    {
        base.Configure(builder);
        builder.ToTable(nameof(Battery));

        builder.HasIndex(x => x.Id).IsUnique();
        
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.CountBookings).IsRequired();
        builder.Property(x => x.ListComments).IsRequired();

        // Optional properties
        builder
            .HasOne(x => x.CurrentUser)
            .WithOne(x => x.CurrentBattery)
            .HasForeignKey<User>(x => x.CurrentBatteryId)
            .IsRequired(false);
        
        builder
            .HasOne(x => x.BatteryBuutAgent)
            .WithOne(x => x.IsBuutAgentOfBattery)
            .HasForeignKey<User>(x => x.IsBuutAgentOfBatteryId)
            .IsRequired(false);
    }
}