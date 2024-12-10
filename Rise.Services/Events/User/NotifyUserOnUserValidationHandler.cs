using Rise.Shared.Enums;
using Rise.Shared.Notifications;
using Rise.Shared.Users;
using Microsoft.Extensions.Logging;

namespace Rise.Services.Events.User;

/// <summary>
/// Handles the UserUpdatedEvent to notify the user about their validation status.
/// </summary>
public class NotifyUserOnUserValidationHandler : IEventHandler<UserValidationEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotifyUserOnUserValidationHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyUserOnUserValidationHandler"/> class.
    /// </summary>
    /// <param name="notificationService">The notification service.</param>
    /// <param name="userService">The user service.</param>
    /// <param name="logger">The logger instance.</param>
    public NotifyUserOnUserValidationHandler(
        INotificationService notificationService,
        IUserService userService,
        IEmailService emailService, 
        ILogger<NotifyUserOnUserValidationHandler> logger)
    {
        _notificationService = notificationService;
        _emailService = emailService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the UserUpdatedEvent to notify the user.
    /// </summary>
    /// <param name="event">The user updated event.</param>
    public async Task HandleAsync(UserValidationEvent @event)
    {
        try
        {
            _logger.LogInformation("Handling UserUpdatedEvent for User ID: {UserId}, Name: {FirstName} {LastName}",
                @event.UserId, @event.FirstName, @event.LastName);

            // Fetch the user to ensure the user exists
            var user = await _userService.GetUserByIdAsync(@event.UserId);
            if (user == null)
            {
                _logger.LogWarning("User with ID: {UserId} not found while handling UserUpdatedEvent.", @event.UserId);
                return;
            }

            // Notify the user about their validation status
            await NotifyUserAsync(user, @event);

            _logger.LogInformation("Successfully notified User ID: {UserId} about their validation status.", @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while handling UserUpdatedEvent for User ID: {UserId}", @event.UserId);
            throw; // Rethrow for upstream handling if necessary
        }
    }

    private async Task NotifyUserAsync(UserDto.UserBase user, UserValidationEvent @event)
    {

        var notification = new NotificationDto.NewNotification
        {
            UserId = user.Id,
            Title_EN = $"User Validation Updated",
            Title_NL = $"Gebruikersvalidatie bijgewerkt",
            Message_EN = $"Dear {user.FirstName}, your account has been validated. You can start your BUUT journey and book your first boat trip.",
            Message_NL = $"Beste {user.FirstName}, uw account is gevalideerd. Je kan beginnen aan jouw BUUT avontuur en je eerste boot trip boeken.",
            RelatedEntityId = @event.UserId,
            Type = NotificationType.UserRegistration
        };

        var email = new EmailMessage
        {
            To = user.Email,
            Subject = "Account Validated",
            Title_EN = notification.Title_EN,
            Title_NL = notification.Title_NL,
            Message_EN = notification.Message_EN,
            Message_NL = notification.Message_NL
        };

        try
        {
            await _notificationService.CreateNotificationAsync(notification);
            _logger.LogInformation("Notification sent to User ID: {UserId} for validation update.", user.Id);

              await _emailService.SendEmailAsync(email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to User ID: {UserId} for validation update.", user.Id);
            throw; // Optionally rethrow if failure to notify the user should stop the process
        }
    }
}
