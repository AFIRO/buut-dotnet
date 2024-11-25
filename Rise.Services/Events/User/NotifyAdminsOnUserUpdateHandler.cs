using Rise.Shared.Enums;
using Rise.Shared.Notifications;
using Rise.Shared.Users;
using Microsoft.Extensions.Logging;

namespace Rise.Services.Events.User;

/// <summary>
/// Handles the UserUpdatedEvent to notify admins about user updates.
/// </summary>
public class NotifyAdminsOnUserUpdateHandler : IEventHandler<UserUpdatedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IUserService _userService;
    private readonly ILogger<NotifyAdminsOnUserUpdateHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyAdminsOnUserUpdateHandler"/> class.
    /// </summary>
    /// <param name="notificationService">The notification service.</param>
    /// <param name="userService">The user service.</param>
    /// <param name="logger">The logger instance.</param>
    public NotifyAdminsOnUserUpdateHandler(
        INotificationService notificationService,
        IUserService userService,
        ILogger<NotifyAdminsOnUserUpdateHandler> logger)
    {
        _notificationService = notificationService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the UserUpdatedEvent to notify all admin users.
    /// </summary>
    /// <param name="event">The user updated event.</param>
    public async Task HandleAsync(UserUpdatedEvent @event)
    {
        try
        {
            _logger.LogInformation("Handling UserUpdatedEvent for User ID: {UserId}, Name: {FirstName} {LastName}",
                @event.UserId, @event.FirstName, @event.LastName);

            var admins = await _userService.GetFilteredUsersAsync(new UserFilter { Role = RolesEnum.Admin });
            if (!admins.Any())
            {
                _logger.LogWarning("No admins found to notify for UserUpdatedEvent with User ID: {UserId}", @event.UserId);
                return;
            }

            var adminNotifications = admins.Select(admin => NotifyAdminAsync(admin, @event));
            await Task.WhenAll(adminNotifications); // Notify all admins in parallel

            _logger.LogInformation("Successfully notified admins about the update of User ID: {UserId}", @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while handling UserUpdatedEvent for User ID: {UserId}", @event.UserId);
            throw; // Rethrow for upstream handling if necessary
        }
    }

    private async Task NotifyAdminAsync(UserDto.UserBase admin, UserUpdatedEvent @event)
    {
        var notification = new NotificationDto.NewNotification
        {
            UserId = admin.Id,
            Title_EN = $"User Updated: {@event.FirstName} {@event.LastName}",
            Title_NL = $"Gebruiker bijgewerkt: {@event.FirstName} {@event.LastName}",
            Message_EN = $"The user {@event.FirstName} {@event.LastName} has updated their profile.",
            Message_NL = $"De gebruiker {@event.FirstName} {@event.LastName} heeft zijn/haar profiel bijgewerkt.",
            RelatedEntityId = @event.UserId,
            Type = NotificationType.Alert
        };

        try
        {
            await _notificationService.CreateNotificationAsync(notification);
            _logger.LogInformation("Notification sent to Admin ID: {AdminId} for User ID: {UserId} update.", admin.Id, @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to Admin ID: {AdminId} for User ID: {UserId} update.", admin.Id, @event.UserId);
            throw; // Optionally rethrow if failure to notify an admin should stop the process
        }
    }
}
