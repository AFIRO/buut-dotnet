using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rise.Domain.Notifications;
using Rise.Persistence;
using Rise.Shared.Enums;
using Rise.Shared.Notifications;

namespace Rise.Services.Notifications;
/// <summary>
/// Service for managing notifications.
/// </summary>
public class NotificationService : INotificationService
{

    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<NotificationService> _logger;
    private readonly IEmailService _emailService;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationService"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    public NotificationService(ApplicationDbContext dbContext, ILogger<NotificationService> logger, IEmailService emailService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _emailService = emailService;
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }


    /// <summary>
    /// Retrieves all notifications.
    /// </summary>
    /// <param name="language">The language for localization.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of view notifications.</returns>
    public async Task<IEnumerable<NotificationDto.ViewNotification>?> GetAllNotificationsAsync(string language = "en")
    {
        try
        {
            // Query all notifications that are not deleted
            var notifications = await _dbContext.Notifications
                .Where(n => !n.IsDeleted)
                .ToListAsync();

            // Transform each notification to a ViewNotification DTO
            var viewNotifications = notifications.Select(n => CreateViewNotification(n, language));

            _logger.LogInformation("All notifications retrieved successfully.");
            return viewNotifications;
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError("Invalid input: {message}.", ex.Message);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Invalid operation: {message}.", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error fetching all notifications: {message}.", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Retrieves all notifications for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="language">The language for localization.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of view notifications.</returns>
    public async Task<IEnumerable<NotificationDto.ViewNotification>?> GetAllUserNotifications(string userId, string language = "en")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogError("User ID cannot be null or empty.");
                throw new ArgumentNullException(nameof(userId), "User ID cannot be null or empty.");
            }
            // Query the notifications
            var query = await _dbContext.Notifications
                .Where(n => n.UserId == userId && !n.IsDeleted).ToListAsync();

            // Transform to ViewNotification
            var notifications = query
                .Select(n => CreateViewNotification(n, language))
                .OrderByDescending(n => n.CreatedAt) // Primary sorting by CreatedAt descending
                .ThenBy(n => n.IsRead); // Secondary sorting: unread (IsRead == false) comes before read

            _logger.LogInformation("Notifications retrieved successfully.");
            return notifications;
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError("Invalid input: {message}.", ex.Message);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Invalid operation: {message}.", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error fetching user notifications: {message}.", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Creates a new notification.
    /// </summary>
    /// <param name="notification">The new notification data.</param>
    /// <param name="language">The language for localization.</param>
    /// <param name="sendEmail">A boolean indicating whether to send an email notification.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created view notification.</returns>
    public async Task<NotificationDto.ViewNotification> CreateNotificationAsync(NotificationDto.NewNotification notification, string language = "en", bool sendEmail = false)
    {
        try
        {
            // Validate the incoming notification data
            if (notification == null)
            {
                _logger.LogError("Notification data cannot be null.");
                throw new ArgumentNullException(nameof(notification), "Notification data cannot be null");
            }

            // Create a new Notification entity
            var newNotification = new Notification(
                userId: notification.UserId,
                title_EN: notification.Title_EN,
                title_NL: notification.Title_NL,
                message_EN: notification.Message_EN,
                message_NL: notification.Message_NL,
                type: notification.Type,
                relatedEntityId: notification.RelatedEntityId ?? null);


            // Add the new notification to the database context
            await _dbContext.Notifications.AddAsync(newNotification);

            // Save changes to the database
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Notification created successfully.");

            // Send email notification if sendEmail is true
            if (sendEmail)
            {
                var user = await _dbContext.Users.FindAsync(notification.UserId);
                if (user != null)
                {
                    var emailMessage = new EmailMessageDto
                    {
                        To = user.Email,
                        Subject = "New Notification",
                        Title_EN = notification.Title_EN,
                        Title_NL = notification.Title_NL,
                        Message_EN = notification.Message_EN,
                        Message_NL = notification.Message_NL
                    };

                    await _emailService.SendEmailAsync(emailMessage);
                }
            }

            // Return the created notification as a ViewNotification DTO
            return CreateViewNotification(newNotification, language);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError("Invalid input: {message}.", ex.Message);
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError("Database update error: {message}.", ex.Message);
            throw new InvalidOperationException("An error occurred while saving the notification to the database.", ex);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Invalid operation: {message}.", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating notification: {message}.", ex.Message);
            throw new Exception("An unexpected error occurred while creating the notification.", ex);
        }
    }

    /// <summary>
    /// Deletes a notification by its ID.
    /// </summary>
    /// <param name="notificationId">The ID of the notification to delete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the deletion was successful.</returns>
    public async Task<bool> DeleteNotificationAsync(string notificationId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(notificationId))
            {
                _logger.LogError("Notification ID cannot be null or empty.");
                throw new ArgumentNullException(nameof(notificationId), "Notification ID cannot be null or empty.");
            }

            // Find the notification by ID
            var notification = await _dbContext.Notifications.FindAsync(notificationId);

            if (notification is null)
            {
                _logger.LogError("Notification with ID {notificationId} not found.", notificationId);
                return false; // Notification not found
            }

            // Perform a soft delete
            notification.IsDeleted = true;

            // Save changes to the database
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Notification deleted successfully.");
            return true; // Successfully deleted
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError("Invalid input: {message}.", ex.Message);
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError("Database update error: {message}.", ex.Message);
            throw new InvalidOperationException("An error occurred while deleting the notification from the database.", ex);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Invalid operation: {message}.", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error deleting notification: {message}.", ex.Message);
            throw new Exception("An unexpected error occurred while deleting the notification.", ex);
        }
    }

    /// <summary>
    /// Updates an existing notification.
    /// </summary>
    /// <param name="notificationDto">The notification data to update.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the update was successful.</returns>
    public async Task<bool> UpdateNotificationAsync(NotificationDto.UpdateNotification notificationDto)
    {
        try
        {
            // Find the notification by ID
            var notification = await _dbContext.Notifications.FindAsync(notificationDto.NotificationId);

            // Check if the notification exists
            if (notification is null)
            {
                _logger.LogError("Notification with ID {notificationId} not found.", notificationDto.NotificationId);
                return false; // Notification not found
            }

            // Update the notification properties
            notification.Title_EN = notificationDto.Title_EN ?? notification.Title_EN;
            notification.Title_NL = notificationDto.Title_NL ?? notification.Title_NL;
            notification.Message_EN = notificationDto.Message_EN ?? notification.Message_EN;
            notification.Message_NL = notificationDto.Message_NL ?? notification.Message_NL;
            if (notificationDto.IsRead != notification.IsRead)
            {
                notification.IsRead = notificationDto.IsRead;
            }
            if (notificationDto.Type.HasValue && notificationDto.Type != notification.Type)
            {
                notification.Type = notificationDto.Type.Value;
            }

            // Save changes to the database
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Notification updated successfully.");
            return true; // Successfully updated
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError("Invalid input: {message}.", ex.Message);
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError("Database update error: {message}.", ex.Message);
            throw new InvalidOperationException("An error occurred while updating the notification in the database.", ex);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Invalid operation: {message}.", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error updating notification: {message}.", ex.Message);
            throw new Exception("An unexpected error occurred while updating the notification.", ex);
        }
    }


    /// <summary>
    /// Retrieves a notification by its ID.
    /// </summary>
    /// <param name="notificationId">The ID of the notification.</param>
    /// <param name="language">The language for localization.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the view notification.</returns>
    public async Task<NotificationDto.ViewNotification?> GetNotificationById(string notificationId, string language = "en")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(notificationId))
            {
                _logger.LogError("Notification ID cannot be null or empty.");
                throw new ArgumentNullException(nameof(notificationId), "Notification ID cannot be null or empty.");
            }

            // Find the notification by ID
            var notification = await _dbContext.Notifications.FindAsync(notificationId);

            // Check if the notification exists
            if (notification is null || notification.IsDeleted)
            {
                _logger.LogError("Notification with ID {notificationId} not found or is deleted.", notificationId);
                return null; // Notification not found or is deleted
            }

            _logger.LogInformation("Notification retrieved successfully.");
            // Transform to ViewNotification
            return CreateViewNotification(notification, language);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError("Invalid input: {message}.", ex.Message);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Invalid operation: {message}.", ex.Message);
            throw new Exception("An error occurred while retrieving the notification.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error fetching notification: {message}.", ex.Message);
            throw new Exception("An unexpected error occurred while fetching the notification.", ex);
        }
    }


    /// <summary>
    /// Retrieves unread notifications for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="language">The language for localization.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of view notifications.</returns>
    public async Task<IEnumerable<NotificationDto.ViewNotification>?> GetUnreadUserNotifications(string userId, string language = "en")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogError("User ID cannot be null or empty.");
                throw new ArgumentNullException(nameof(userId), "User ID cannot be null or empty.");
            }

            // Query unread notifications for the user
            var notifications = await _dbContext.Notifications
                .Where(n => n.UserId == userId && !n.IsDeleted && !n.IsRead)
                .ToListAsync();

            // Transform each notification to a ViewNotification DTO
            var viewNotifications = notifications.Select(n => CreateViewNotification(n, language));

            _logger.LogInformation("Unread notifications retrieved successfully.");
            return viewNotifications;
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError("Invalid input: {message}.", ex.Message);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Invalid operation: {message}.", ex.Message);
            throw new Exception("An error occurred while retrieving unread notifications.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error fetching unread notifications: {message}.", ex.Message);
            throw new Exception("An unexpected error occurred while fetching unread notifications.", ex);
        }
    }


    /// <summary>
    /// Retrieves read notifications for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="language">The language for localization.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of view notifications.</returns>
    public async Task<IEnumerable<NotificationDto.ViewNotification>?> GetReadUserNotifications(string userId, string language = "en")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogError("User ID cannot be null or empty.");
                throw new ArgumentNullException(nameof(userId), "User ID cannot be null or empty.");
            }

            // Query read notifications for the user
            var notifications = await _dbContext.Notifications
                .Where(n => n.UserId == userId && !n.IsDeleted && n.IsRead)
                .ToListAsync();

            // Transform each notification to a ViewNotification DTO
            var viewNotifications = notifications.Select(n => CreateViewNotification(n, language)).OrderByDescending(n => n.CreatedAt);

            _logger.LogInformation("Read notifications retrieved successfully.");
            return viewNotifications;
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError("Invalid input: {message}.", ex.Message);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Invalid operation: {message}.", ex.Message);
            throw new Exception("An error occurred while retrieving read notifications.", ex);
        }

        catch (Exception ex)
        {
            _logger.LogError("Error fetching read notifications: {message}.", ex.Message);
            throw new Exception("An unexpected error occurred while fetching read notifications.", ex);
        }
    }


    /// <summary>
    /// Retrieves notifications of a specific type for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="type">The type of notifications to retrieve.</param>
    /// <param name="language">The language for localization.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of view notifications.</returns>
    public async Task<IEnumerable<NotificationDto.ViewNotification>?> GetUserNotificationsByType(string userId, NotificationType type, string language = "en")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogError("User ID cannot be null or empty.");
                throw new ArgumentNullException(nameof(userId), "User ID cannot be null or empty.");
            }

            // Query notifications for the user filtered by type
            var notifications = await _dbContext.Notifications
                .Where(n => n.UserId == userId && !n.IsDeleted && n.Type == type)
                .ToListAsync();

            // Transform each notification to a ViewNotification DTO
            var viewNotifications = notifications.Select(n => CreateViewNotification(n, language));

            _logger.LogInformation("Notifications by type retrieved successfully.");
            return viewNotifications;
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError("Invalid input: {message}.", ex.Message);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Invalid operation: {message}.", ex.Message);
            throw new Exception("An error occurred while retrieving notifications by type.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error fetching notifications by type: {message}.", ex.Message);
            throw new Exception("An unexpected error occurred while fetching notifications by type.", ex);
        }
    }

    private NotificationDto.ViewNotification CreateViewNotification(Notification notification, string language = "en")
    {
        try
        {
            return new NotificationDto.ViewNotification
            {
                NotificationId = notification.Id,
                Title = GetLocalizedText(notification.Title_NL, notification.Title_EN, language),
                Message = GetLocalizedText(notification.Message_NL, notification.Message_EN, language),
                IsRead = notification.IsRead,
                Type = notification.Type,
                CreatedAt = notification.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating view notification: {message}.", ex.Message);
            throw;
        }
    }

    private string GetLocalizedText(string textNL, string textEN, string language)
    {
        try
        {
            if (language.Contains("nl", StringComparison.OrdinalIgnoreCase))
            {
                return textNL;
            }
            if (language.Contains("en", StringComparison.OrdinalIgnoreCase))
            {
                return textEN;
            }
            return textEN; // Default to English if the language is not specified or not recognized
        }
        catch (Exception ex)
        {
            _logger.LogError("Error fetching localized text: {message}.", ex.Message);
            // Return a default value to ensure the application continues running
            return textEN;
        }
    }

    /// <summary>
    /// Retrieves the count of unread notifications for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the count of unread notifications.</returns>
    public async Task<NotificationDto.NotificationCount> GetUnreadUserNotificationsCount(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogError("User ID cannot be null or empty.");
                throw new ArgumentNullException(nameof(userId), "User ID cannot be null or empty.");
            }

            var unreadCount = await _dbContext.Notifications
                .Where(n => n.UserId == userId && !n.IsDeleted && !n.IsRead)
                .CountAsync();

            _logger.LogInformation("Unread notifications count retrieved successfully.");
            return new NotificationDto.NotificationCount { Count = unreadCount };
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError("Invalid input: {message}.", ex.Message);
            throw; // Re-throw the exception for higher-level handling
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Invalid operation: {message}.", ex.Message);
            throw; // Rethrow to propagate the specific issue
        }
        catch (Exception ex)
        {
            _logger.LogError("Error fetching unread notifications count: {message}.", ex.Message);
            throw new Exception("An unexpected error occurred while fetching unread notifications count.", ex);
        }
    }
}