using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rise.Shared.Enums;
using Rise.Shared.Notifications;
using Rise.Shared.Users;

namespace Rise.Services.Events.User;

/// <summary>
/// Handles the UserRoleUpdatedEvent to notify the user about their new roles.
/// </summary>
public class NotifyUserOnNewRolesAssignedHandler : IEventHandler<UserRoleUpdatedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotifyUserOnNewRolesAssignedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyUserOnNewRolesAssignedHandler"/> class.
    /// </summary>
    /// <param name="notificationService">The notification service.</param>
    /// <param name="logger">The logger instance.</param>
    public NotifyUserOnNewRolesAssignedHandler(
        INotificationService notificationService,
        ILogger<NotifyUserOnNewRolesAssignedHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the UserRoleUpdatedEvent to notify the user about their new roles.
    /// </summary>
    /// <param name="event">The UserRoleUpdatedEvent instance.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task HandleAsync(UserRoleUpdatedEvent @event)
    {
        try
        {
            _logger.LogInformation("Handling UserRoleUpdatedEvent for User ID: {UserId}", @event.UserId);

            var newRoles = string.Join(", ", @event.NewRoles.Select(r => r.ToString()));
            var oldRoles = string.Join(", ", @event.OldRoles.Select(r => r.ToString()));

            var notification = new NotificationDto.NewNotification
            {
                UserId = @event.UserId,
                Title_EN = "Your Roles Have Been Updated",
                Title_NL = "Uw rollen zijn bijgewerkt",
                Message_EN = $"Your roles have been updated from [{oldRoles}] to [{newRoles}].",
                Message_NL = $"Uw rollen zijn bijgewerkt van [{oldRoles}] naar [{newRoles}].",
                RelatedEntityId = @event.UserId,
                Type = NotificationType.Alert
            };

            await _notificationService.CreateNotificationAsync(notification);

            _logger.LogInformation("Notification sent to User ID: {UserId} for role update.", @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while handling UserRoleUpdatedEvent for User ID: {UserId}", @event.UserId);
            throw;
        }
    }
}
