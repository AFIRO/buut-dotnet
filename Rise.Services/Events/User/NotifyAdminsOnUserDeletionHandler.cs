using Rise.Shared.Enums;
using Rise.Shared.Notifications;
using Rise.Shared.Users;
using Microsoft.Extensions.Logging;

namespace Rise.Services.Events.User;

/// <summary>
/// Handles the UserDeletedEvent to notify admins.
/// </summary>
public class NotifyAdminsOnUserDeletionHandler : IEventHandler<UserDeletedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IUserService _userService;
    private readonly ILogger<NotifyAdminsOnUserDeletionHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyAdminsOnUserDeletionHandler"/> class.
    /// </summary>
    /// <param name="notificationService">The notification service.</param>
    /// <param name="userService">The user service.</param>
    /// <param name="logger">The logger instance.</param>
    public NotifyAdminsOnUserDeletionHandler(
        INotificationService notificationService,
        IUserService userService,
        ILogger<NotifyAdminsOnUserDeletionHandler> logger)
    {
        _notificationService = notificationService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the UserDeletedEvent to notify admins about user deletion.
    /// </summary>
    /// <param name="event">The user deleted event.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task HandleAsync(UserDeletedEvent @event)
    {
        try
        {
            _logger.LogInformation("Handling UserDeletedEvent for User ID: {UserId}, Name: {FirstName} {LastName}", 
                @event.UserId, @event.FirstName, @event.LastName);

            var admins = await _userService.GetFilteredUsersAsync(new UserFilter { Role = RolesEnum.Admin });
            if (!admins.Any())
            {
                _logger.LogWarning("No admins found to notify for UserDeletedEvent with User ID: {UserId}", @event.UserId);
                return;
            }

            var adminNotifications = admins.Select(admin => NotifyAdminAsync(admin, @event));
            await Task.WhenAll(adminNotifications); // Notify all admins in parallel

            _logger.LogInformation("Successfully notified admins about the deletion of User ID: {UserId}", @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while handling UserDeletedEvent for User ID: {UserId}", @event.UserId);
            throw; // Rethrow for upstream handling if necessary
        }
    }

    private async Task NotifyAdminAsync(UserDto.UserBase admin, UserDeletedEvent @event)
    {
        var notification = new NotificationDto.NewNotification
        {
            UserId = admin.Id,
            Title_EN = $"User Deleted : {@event.FirstName} {@event.LastName}",
            Title_NL = $"Gebruiker verwijderd: {@event.FirstName} {@event.LastName}",
            Message_EN = $"The user {@event.FirstName} {@event.LastName} (ID: {@event.UserId}) has initiated their own deletion.",
            Message_NL = $"De gebruiker {@event.FirstName} {@event.LastName} (ID: {@event.UserId}) heeft zijn eigen verwijdering ingediend.",
            RelatedEntityId = @event.UserId,
            Type = NotificationType.Alert
        };

        try
        {
            await _notificationService.CreateNotificationAsync(notification);
            _logger.LogInformation("Notification sent to Admin ID: {AdminId} for User ID: {UserId}", admin.Id, @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to Admin ID: {AdminId} for User ID: {UserId}", admin.Id, @event.UserId);
            throw; // Optionally rethrow if failure to notify an admin should stop the process
        }
    }
}
