using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.Bookings;
using Rise.Domain.Notifications;
using Rise.Domain.Users;

namespace Rise.Persistence.Users;

/// <summary>
/// Provides configuration settings for the <see cref="User"/> entity in the persistence layer.
/// </summary>
internal class UserConfiguration : EntityConfiguration<User>
{
    /// <summary>
    /// Configures the <see cref="User"/> entity using the specified <see cref="EntityTypeBuilder{TEntity}"/>.
    /// </summary>
    /// <param name="builder">The builder used to configure the <see cref="User"/> entity.</param>
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);
        builder.ToTable(nameof(User));

        builder.HasIndex(x => x.Id).IsUnique();

        builder.Property(x => x.FirstName).IsRequired();
        builder.Property(x => x.LastName).IsRequired();
        builder.Property(x => x.Email).IsRequired();
        builder.Property(x => x.BirthDate).IsRequired();
        builder.Property(x => x.PhoneNumber).IsRequired();

        builder
            .HasOne(x => x.Address)
            .WithOne(x => x.User)
            .HasForeignKey<Address>(x => x.UserId)
            .IsRequired();

        builder
            .HasMany(x => x.Bookings)
            .WithOne()
            .HasForeignKey(x => x.UserId);

        builder
            .HasMany(x => x.Notifications)
            .WithOne()
            .HasForeignKey(x => x.UserId);
    }
}
