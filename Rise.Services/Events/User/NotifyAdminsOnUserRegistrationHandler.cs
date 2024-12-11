using Rise.Shared.Enums;
using Rise.Shared.Notifications;
using Rise.Shared.Users;
using Microsoft.Extensions.Logging;

namespace Rise.Services.Events.User;

/// <summary>
/// Handles the UserRegisteredEvent to notify admins.
/// </summary>
public class NotifyAdminsOnUserRegistrationHandler : IEventHandler<UserRegisteredEvent>
{
    private readonly INotificationService _notificationService;

    private readonly IEmailService _emailService;
    private readonly IUserService _userService;
    private readonly ILogger<NotifyAdminsOnUserRegistrationHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyAdminsOnUserRegistrationHandler"/> class.
    /// </summary>
    /// <param name="notificationService">The notification service.</param>
    /// <param name="userService">The user service.</param>
    /// <param name="logger">The logger instance.</param>
    public NotifyAdminsOnUserRegistrationHandler(
        INotificationService notificationService,
        IUserService userService,
        IEmailService emailService,
        ILogger<NotifyAdminsOnUserRegistrationHandler> logger)
    {
        _notificationService = notificationService;
        _userService = userService;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the UserRegisteredEvent to notify all admin users.
    /// </summary>
    /// <param name="event">The user registered event.</param>
    public async Task HandleAsync(UserRegisteredEvent @event)
    {
        try
        {
            _logger.LogInformation("Handling UserRegisteredEvent for User ID: {UserId}, Name: {FirstName} {LastName}",
                @event.UserId, @event.FirstName, @event.LastName);

            var admins = await _userService.GetFilteredUsersAsync(new UserFilter { Role = RolesEnum.Admin });
            if (!admins.Any())
            {
                _logger.LogWarning("No admins found to notify for UserRegisteredEvent with User ID: {UserId}", @event.UserId);
                return;
            }

            var adminNotifications = admins.Select(admin => NotifyAdminAsync(admin, @event));
            await Task.WhenAll(adminNotifications); // Notify all admins in parallel

            _logger.LogInformation("Successfully notified admins about the registration of User ID: {UserId}", @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while handling UserRegisteredEvent for User ID: {UserId}", @event.UserId);
            throw; // Rethrow for upstream handling if necessary
        }
    }

    private async Task NotifyAdminAsync(UserDto.UserBase admin, UserRegisteredEvent @event)
    {
        var notification = new NotificationDto.NewNotification
        {
            UserId = admin.Id,
            Title_EN = $"New User Registered: {@event.FirstName} {@event.LastName}",
            Title_NL = $"Nieuwe gebruiker geregistreerd: {@event.FirstName} {@event.LastName}",
            Message_EN = $"A new user {@event.FirstName} {@event.LastName} has registered.",
            Message_NL = $"Een nieuwe gebruiker {@event.FirstName} {@event.LastName} heeft zich geregistreerd.",
            RelatedEntityId = @event.UserId,
            Type = NotificationType.UserRegistration
        };

        var email = new EmailMessage
        {
            To = admin.Email,
            Subject = "New User Registered",
            Title_EN = notification.Title_EN,
            Title_NL = notification.Title_NL,
            Message_EN = notification.Message_EN,
            Message_NL = notification.Message_NL
        };

        try
        {
            await _notificationService.CreateNotificationAsync(notification);
            _logger.LogInformation("Notification sent to Admin ID: {AdminId} for new User ID: {UserId}", admin.Id, @event.UserId);
            await _emailService.SendEmailAsync(email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to Admin ID: {AdminId} for new User ID: {UserId}", admin.Id, @event.UserId);
            throw; // Optionally rethrow if failure to notify an admin should stop the process
        }
    }
}
