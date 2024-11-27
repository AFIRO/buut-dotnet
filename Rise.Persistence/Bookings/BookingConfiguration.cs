using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.Bookings;

namespace Rise.Persistence.Bookings;

internal class BookingConfiguration : EntityConfiguration<Booking>
{
    /// <summary>
    /// Configures the <see cref="Booking"/> entity using the specified <see cref="EntityTypeBuilder"/>.
    /// </summary>
    /// <param name="builder">The builder used to configure the <see cref="Booking"/> entity.</param>
    public override void Configure(EntityTypeBuilder<Booking> builder)
    {
        base.Configure(builder);
        builder.ToTable(nameof(Booking));
        

        builder.Property(x => x.BookingDate).IsRequired();
        builder.Property(x => x.BookingDate).IsRequired();

        // Optional properties - no need to set as required
        builder.Property(x => x.BoatId);
        builder.Property(x => x.BatteryId);

        builder
            .HasOne(x => x.Boat)
            .WithMany()
            .HasForeignKey(x => x.BoatId);
        
        builder
            .HasOne(x => x.Battery)
            .WithMany()
            .HasForeignKey(x => x.BatteryId);
    }
}