using Rise.Shared.Enums;
using Rise.Shared.Notifications;
using Rise.Shared.Users;
using Microsoft.Extensions.Logging;

namespace Rise.Services.Events.Booking;

/// <summary>
/// Handles the event when a booking is updated.
/// </summary>
public class NotifyOnBookingUpdatedHandler : IEventHandler<BookingUpdatedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IUserService _userService;
    private readonly ILogger<NotifyOnBookingUpdatedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyOnBookingUpdatedHandler"/> class.
    /// </summary>
    /// <param name="notificationService">The notification service.</param>
    /// <param name="userService">The user service.</param>
    /// <param name="logger">The logger instance.</param>
    public NotifyOnBookingUpdatedHandler(
        INotificationService notificationService,
        IUserService userService,
        ILogger<NotifyOnBookingUpdatedHandler> logger)
    {
        _notificationService = notificationService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the booking updated event.
    /// </summary>
    /// <param name="event">The booking updated event.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task HandleAsync(BookingUpdatedEvent @event)
    {
        try
        {
            await NotifyUser(@event);
            await NotifyAdmins(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while handling the BookingUpdatedEvent for Booking ID: {BookingId}", @event.BookingId);
            throw; // Rethrow to allow upstream handling if necessary
        }
    }

    private async Task NotifyUser(BookingUpdatedEvent @event)
    {
        var notification = new NotificationDto.NewNotification
        {
            UserId = @event.UserId,
            Title_EN = "Booking Updated",
            Title_NL = "Reservering bijgewerkt",
            Message_EN = $"Your booking for {@event.OldBookingDate:dd/MM/yyyy} at {@event.OldTimeSlot} has been updated to {@event.NewBookingDate:dd/MM/yyyy} at {@event.NewTimeSlot}.",
            Message_NL = $"Uw reservering voor {@event.OldBookingDate:dd/MM/yyyy} om {@event.OldTimeSlot} is bijgewerkt naar {@event.NewBookingDate:dd/MM/yyyy} om {@event.NewTimeSlot}.",
            RelatedEntityId = @event.BookingId,
            Type = NotificationType.Booking
        };

        await CreateNotificationWithLoggingAsync(notification, "user");
    }

    private async Task NotifyAdmins(BookingUpdatedEvent @event)
    {
        var user = await _userService.GetUserByIdAsync(@event.UserId);
        var admins = await _userService.GetFilteredUsersAsync(new UserFilter { Role = RolesEnum.Admin });

        var adminNotifications = admins.Select(admin =>
        {
            var notification = new NotificationDto.NewNotification
            {
                UserId = admin.Id,
                Title_EN = "Booking Updated",
                Title_NL = "Reservering bijgewerkt",
                Message_EN = $"A booking for {@event.OldBookingDate:dd/MM/yyyy} at {@event.OldTimeSlot} has been updated to {@event.NewBookingDate:dd/MM/yyyy} at {@event.NewTimeSlot} by {user?.FirstName ?? "Unknown"} {user?.LastName ?? "User"}.",
                Message_NL = $"Een reservering voor {@event.OldBookingDate:dd/MM/yyyy} om {@event.OldTimeSlot} is bijgewerkt naar {@event.NewBookingDate:dd/MM/yyyy} om {@event.NewTimeSlot} door {user?.FirstName ?? "Onbekende"} {user?.LastName ?? "Gebruiker"}.",
                RelatedEntityId = @event.BookingId,
                Type = NotificationType.Booking
            };

            return CreateNotificationWithLoggingAsync(notification, "admin", admin.Id);
        });

        await Task.WhenAll(adminNotifications); // Notify all admins in parallel
    }

    private async Task CreateNotificationWithLoggingAsync(NotificationDto.NewNotification notification, string recipientType, string? recipientId = null)
    {
        try
        {
            await _notificationService.CreateNotificationAsync(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification for {RecipientType} with ID: {RecipientId}", recipientType, recipientId ?? notification.UserId);
            throw;
        }
    }
}
