using Rise.Shared.Enums;
using Rise.Shared.Notifications;
using Rise.Shared.Users;
using Microsoft.Extensions.Logging;

namespace Rise.Services.Events.Booking;

/// <summary>
/// Handles the event when a booking is created.
/// </summary>
public class NotifyOnBookingCreatedHandler : IEventHandler<BookingCreatedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IUserService _userService;
    private readonly ILogger<NotifyOnBookingCreatedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyOnBookingCreatedHandler"/> class.
    /// </summary>
    /// <param name="notificationService">The notification service.</param>
    /// <param name="userService">The user service.</param>
    /// <param name="logger">The logger instance.</param>
    public NotifyOnBookingCreatedHandler(
        INotificationService notificationService,
        IUserService userService,
        ILogger<NotifyOnBookingCreatedHandler> logger)
    {
        _notificationService = notificationService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the booking created event.
    /// </summary>
    /// <param name="event">The booking created event.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task HandleAsync(BookingCreatedEvent @event)
    {
        try
        {
            await NotifyUser(@event);
            await NotifyAdmins(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while handling the BookingCreatedEvent for Booking ID: {BookingId}", @event.BookingId);
            throw; // Rethrow to allow upstream handling if necessary
        }
    }

    private async Task NotifyUser(BookingCreatedEvent @event)
    {
        var notification = new NotificationDto.NewNotification
        {
            UserId = @event.UserId,
            Title_EN = "Booking Created",
            Title_NL = "Reservering aangemaakt",
            Message_EN = $"Your booking for {@event.BookingDate:dd/MM/yyyy} at {@event.TimeSlot} has been created.",
            Message_NL = $"Uw reservering voor {@event.BookingDate:dd/MM/yyyy} om {@event.TimeSlot} is aangemaakt.",
            RelatedEntityId = @event.BookingId,
            Type = NotificationType.Booking
        };

        await CreateNotificationWithLoggingAsync(notification, "user");
    }

    private async Task NotifyAdmins(BookingCreatedEvent @event)
    {
        var user = await _userService.GetUserByIdAsync(@event.UserId);
        var admins = await _userService.GetFilteredUsersAsync(new UserFilter { Role = RolesEnum.Admin });

        var adminNotifications = admins.Select(admin =>
        {
            var notification = new NotificationDto.NewNotification
            {
                UserId = admin.Id,
                Title_EN = "Booking Created",
                Title_NL = "Reservering aangemaakt",
                Message_EN = $"A booking for {@event.BookingDate:dd/MM/yyyy} at {@event.TimeSlot} has been created by {user?.FirstName ?? "Unknown"} {user?.LastName ?? "User"}.",
                Message_NL = $"Een reservering voor {@event.BookingDate:dd/MM/yyyy} om {@event.TimeSlot} is aangemaakt door {user?.FirstName ?? "Onbekende"} {user?.LastName ?? "Gebruiker"}.",
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
