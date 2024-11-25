using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.Notifications;
using Rise.Shared.Enums;

namespace Rise.Persistence.Users;

internal class NotificationConfiguration : EntityConfiguration<Notification>
{
    public override void Configure(EntityTypeBuilder<Notification> builder)
    {
        base.Configure(builder);
        builder.ToTable(nameof(Notification));
        builder.HasIndex(x => x.Id).IsUnique();
        builder.Property(x => x.Title_EN).IsRequired();
        builder.Property(x => x.Title_NL).IsRequired();
        builder.Property(x => x.Message_EN).IsRequired();
        builder.Property(x => x.Message_NL).IsRequired();
        builder.Property(x => x.IsRead).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Type).IsRequired() .HasConversion(
                v => v.ToString(), // Convert RolesEnum to string when saving to the database
                v => (NotificationType)Enum.Parse(typeof(NotificationType), v) // Convert string back to RolesEnum when retrieving from the database
            )
            .HasColumnType("nvarchar(50)"); // Specify the column ;
    }
}