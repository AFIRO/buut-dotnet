using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationService"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public NotificationService(ApplicationDbContext dbContext)
    {
        this._dbContext = dbContext;
        this._jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Retrieves all notifications.
    /// </summary>
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

            return viewNotifications;
        }
        catch (ArgumentNullException ex)
        {
            Console.Error.WriteLine($"Argument Null Exception: {ex.Message}");
            throw;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Invalid Operation Exception: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            // Log the error
            Console.Error.WriteLine($"Error fetching all notifications: {ex.Message}");
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
                throw new ArgumentNullException(nameof(userId), "User ID cannot be null or empty.");
            }
            // Query the notifications
            var query = await _dbContext.Notifications
                .Where(n => n.UserId == userId && !n.IsDeleted).ToListAsync();

            // Transform to ViewNotification
            // var notifications = query
            //     .Select(n => CreateViewNotification(n, language)).GroupBy(n => n.IsRead).SelectMany(g => g).OrderByDescending(n => n.CreatedAt);

            var notifications = query
                .Select(n => CreateViewNotification(n, language))
                .OrderByDescending(n => n.CreatedAt) // Primary sorting by CreatedAt descending
                .ThenBy(n => n.IsRead); // Secondary sorting: unread (IsRead == false) comes before read


            return notifications;
        }
        catch (ArgumentNullException ex)
        {
            Console.Error.WriteLine($"Argument Null Exception: {ex.Message}");
            throw;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Invalid Operation Exception: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            // Log the error (you can use a logging framework like Serilog, NLog, etc.)
            Console.Error.WriteLine($"Error fetching notifications for user {userId}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Creates a new notification.
    /// </summary>
    /// <param name="notification">The new notification data.</param>
    /// <param name="language">The language for localization.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created view notification.</returns>
    public async Task<NotificationDto.ViewNotification> CreateNotificationAsync(NotificationDto.NewNotification notification, string language = "en")
    {
        try
        {
            // Validate the incoming notification data
            if (notification == null)
            {
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

            // Return the created notification as a ViewNotification DTO
            return CreateViewNotification(newNotification, language);
        }
        catch (ArgumentNullException ex)
        {
            // Log the error
            Console.Error.WriteLine($"Argument Null Exception: {ex.Message}");
            throw; // Re-throw the exception to be handled by higher-level error handlers
        }
        catch (DbUpdateException ex)
        {
            // Log the error (specific to database update issues)
            Console.Error.WriteLine($"Database Update Exception: {ex.Message}");
            throw new InvalidOperationException("An error occurred while saving the notification to the database.", ex);
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Invalid Operation Exception: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            // Log any other errors
            Console.Error.WriteLine($"General Exception: {ex.Message}");
            throw new Exception("An unexpected error occurred while creating the notification.", ex);
        }
    }

    /// <summary>
    /// Deletes a notification by its ID.
    /// </summary>
    /// <param name="id">The ID of the notification to delete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the deletion was successful.</returns>
    public async Task<bool> DeleteNotificationAsync(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id), "Notification ID cannot be null or empty.");
            }

            // Find the notification by ID
            var notification = await _dbContext.Notifications.FindAsync(id);

            // Check if the notification exists
            if (notification is null)
            {
                Console.Error.WriteLine($"Notification with ID {id} not found.");
                return false; // Notification not found
            }

            // Perform a soft delete
            notification.IsDeleted = true;

            // Save changes to the database
            await _dbContext.SaveChangesAsync();

            return true; // Successfully deleted
        }
        catch (ArgumentNullException ex)
        {
            Console.Error.WriteLine($"Argument Null Exception: {ex.Message}");
            throw;
        }
        catch (DbUpdateException ex)
        {
            // Log the error (specific to database update issues)
            Console.Error.WriteLine($"Database Update Exception: {ex.Message}");
            throw new InvalidOperationException("An error occurred while deleting the notification from the database.", ex);
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Invalid Operation Exception: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            // Log any other errors
            Console.Error.WriteLine($"General Exception: {ex.Message}");
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
                Console.Error.WriteLine($"Notification with ID {notificationDto.NotificationId} not found.");
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
            if (notificationDto.Type != notification.Type)
            {
                notification.Type = notificationDto.Type;
            }

            // Save changes to the database
            await _dbContext.SaveChangesAsync();

            return true; // Successfully updated
        }
        catch (ArgumentNullException ex)
        {
            Console.Error.WriteLine($"Argument Null Exception: {ex.Message}");
            throw;
        }
        catch (DbUpdateException ex)
        {
            // Log the error (specific to database update issues)
            Console.Error.WriteLine($"Database Update Exception: {ex.Message}");
            throw new InvalidOperationException("An error occurred while updating the notification in the database.", ex);
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Invalid Operation Exception: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            // Log any other errors
            Console.Error.WriteLine($"General Exception: {ex.Message}");
            throw new Exception("An unexpected error occurred while updating the notification.", ex);
        }
    }


    /// <summary>
    /// Retrieves a notification by its ID.
    /// </summary>
    /// <param name="id">The ID of the notification.</param>
    /// <param name="language">The language for localization.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the view notification.</returns>
    public async Task<NotificationDto.ViewNotification?> GetNotificationById(string id, string language = "en")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id), "Notification ID cannot be null or empty.");
            }

            // Find the notification by ID
            var notification = await _dbContext.Notifications.FindAsync(id);

            // Check if the notification exists
            if (notification is null || notification.IsDeleted)
            {
                Console.Error.WriteLine($"Notification with ID {id} not found or is deleted.");
                return null; // Notification not found or is deleted
            }

            // Transform to ViewNotification
            return CreateViewNotification(notification, language);
        }
        catch (ArgumentNullException ex)
        {
            Console.Error.WriteLine($"Argument Null Exception: {ex.Message}");
            throw;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Invalid Operation Exception: {ex.Message}");
            throw new Exception("An error occurred while retrieving the notification.", ex);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"General Exception: {ex.Message}");
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
                throw new ArgumentNullException(nameof(userId), "User ID cannot be null or empty.");
            }

            // Query unread notifications for the user
            var notifications = await _dbContext.Notifications
                .Where(n => n.UserId == userId && !n.IsDeleted && !n.IsRead)
                .ToListAsync();

            // Transform each notification to a ViewNotification DTO
            var viewNotifications = notifications.Select(n => CreateViewNotification(n, language));

            return viewNotifications;
        }
        catch (ArgumentNullException ex)
        {
            Console.Error.WriteLine($"Argument Null Exception: {ex.Message}");
            throw;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Invalid Operation Exception: {ex.Message}");
            throw new Exception("An error occurred while retrieving unread notifications.", ex);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"General Exception: {ex.Message}");
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
                throw new ArgumentNullException(nameof(userId), "User ID cannot be null or empty.");
            }

            // Query read notifications for the user
            var notifications = await _dbContext.Notifications
                .Where(n => n.UserId == userId && !n.IsDeleted && n.IsRead)
                .ToListAsync();

            // Transform each notification to a ViewNotification DTO
            var viewNotifications = notifications.Select(n => CreateViewNotification(n, language)).OrderByDescending(n => n.CreatedAt);

            return viewNotifications;
        }
        catch (ArgumentNullException ex)
        {
            Console.Error.WriteLine($"Argument Null Exception: {ex.Message}");
            throw;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Invalid Operation Exception: {ex.Message}");
            throw new Exception("An error occurred while retrieving read notifications.", ex);
        }

        catch (Exception ex)
        {
            Console.Error.WriteLine($"General Exception: {ex.Message}");
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
                throw new ArgumentNullException(nameof(userId), "User ID cannot be null or empty.");
            }

            // Query notifications for the user filtered by type
            var notifications = await _dbContext.Notifications
                .Where(n => n.UserId == userId && !n.IsDeleted && n.Type == type)
                .ToListAsync();

            // Transform each notification to a ViewNotification DTO
            var viewNotifications = notifications.Select(n => CreateViewNotification(n, language));

            return viewNotifications;
        }
        catch (ArgumentNullException ex)
        {
            Console.Error.WriteLine($"Argument Null Exception: {ex.Message}");
            throw;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Invalid Operation Exception: {ex.Message}");
            throw new Exception("An error occurred while retrieving notifications by type.", ex);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"General Exception: {ex.Message}");
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
            // Log the error (for debugging purposes)
            Console.Error.WriteLine($"Error creating view notification: {ex.Message}");

            // You may choose to handle the exception differently based on your requirements
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
            // Log the error (for debugging purposes)
            Console.Error.WriteLine($"Error selecting localized text: {ex.Message}");

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
                throw new ArgumentNullException(nameof(userId), "User ID cannot be null or empty.");
            }

            var unreadCount = await _dbContext.Notifications
                .Where(n => n.UserId == userId && !n.IsDeleted && !n.IsRead)
                .CountAsync();

            return new NotificationDto.NotificationCount { Count = unreadCount };
        }
        catch (ArgumentNullException ex)
        {
            Console.Error.WriteLine($"Argument Null Exception: {ex.Message}");
            throw; // Re-throw the exception for higher-level handling
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Invalid Operation Exception: {ex.Message}");
            throw; // Rethrow to propagate the specific issue
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error fetching unread notification count: {ex.Message}");
            throw new Exception("An unexpected error occurred while fetching unread notifications count.", ex);
        }
    }
}