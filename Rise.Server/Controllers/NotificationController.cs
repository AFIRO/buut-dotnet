using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rise.Shared.Enums;
using Rise.Shared.Notifications;

namespace Rise.Server.Controllers;

/// <summary>
/// Controller for handling notification-related requests.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    private readonly ILogger<NotificationController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationController"/> class.
    /// </summary>
    /// <param name="notificationService">The notification service.</param>
    /// <param name="logger">The logger instance.</param>
    public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }



    /// <summary>
    /// Retrieves all notifications.
    /// </summary>
    /// <param name="language">The language for the notifications.</param>
    /// <returns>A list of all notifications.</returns>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllNotifications([FromQuery] string language = "en")
    {
        try
        {
            var notifications = await _notificationService.GetAllNotificationsAsync(language);
            _logger.LogInformation("Retrieved all notifications.");
            return Ok(notifications);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError("Invalid input: {message}.", ex.Message);
            return BadRequest(new { message = $"Invalid input: {ex.Message}" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Internal server error: {message}.", ex.Message);
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            _logger.LogError("An unexpected error occurred: {message}.", ex.Message);
            return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
        }
    }


    /// <summary>
    /// Retrieves notifications for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="language">The language for the notifications.</param>
    /// <returns>A list of notifications for the user.</returns>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserNotifications(string userId, [FromQuery] string language = "en")
    {
        try
        {
            var notifications = await _notificationService.GetAllUserNotifications(userId, language);
            _logger.LogInformation("Retrieved all notifications for user with ID {userId}.", userId);
            return Ok(notifications);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError("Invalid input: {message}", ex.Message);
            return BadRequest(new { message = $"Invalid input: {ex.Message}" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Internal server error: {message}", ex.Message);
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            _logger.LogError("An unexpected error occurred: {message}", ex.Message);
            return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
        }
    }


    /// <summary>
    /// Creates a new notification.
    /// </summary>
    /// <param name="notification">The notification data transfer object containing the new notification details.</param>
    /// <param name="language">The language for the notification.</param>
    /// <returns>An IActionResult containing the created notification.</returns>
    [HttpPost]
    public async Task<IActionResult> CreateNotification([FromBody] NotificationDto.NewNotification notification, [FromQuery] string language = "en", [FromQuery] bool sendEmail = false)
    {
        try
        {
            var createdNotification = await _notificationService.CreateNotificationAsync(notification, language, sendEmail);
            _logger.LogInformation("Created new notification with ID {notificationId}.", createdNotification.NotificationId);
            return Ok(createdNotification);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError("Invalid input: {message}", ex.Message);
            return BadRequest(new { message = $"Invalid input: {ex.Message}" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Internal server error: {message}", ex.Message);
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            _logger.LogError("An unexpected error occurred: {message}", ex.Message);
            return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
        }
    }


    /// <summary>
    /// Retrieves a notification by its ID.
    /// </summary>
    /// <param name="notificationId">The ID of the notification to retrieve.</param>
    /// <param name="language">The language for the notification.</param>
    /// <returns>An IActionResult containing the notification.</returns>
    [HttpGet("{notificationId}")]
    public async Task<IActionResult> GetNotificationById(string notificationId, [FromQuery] string language = "en")
    {
        try
        {
            var notification = await _notificationService.GetNotificationById(notificationId, language);
            if (notification == null)
            {
                _logger.LogWarning("Notification with ID {notificationId} not found.", notificationId);
                return NotFound(new { message = $"Notification with ID {notificationId} not found." });
            }
            _logger.LogInformation("Retrieved notification with ID {notificationId}.", notificationId);
            return Ok(notification);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError("Invalid input: {message}", ex.Message);
            return BadRequest(new { message = $"Invalid input: {ex.Message}" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Internal server error: {message}", ex.Message);
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            _logger.LogError("An unexpected error occurred: {message}", ex.Message);
            return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
        }
    }


    /// <summary>
    /// Deletes a notification by its ID.
    /// </summary>
    /// <param name="notificationId">The ID of the notification to delete.</param>
    /// <returns>An IActionResult indicating the result of the delete operation.</returns>
    [HttpDelete("{notificationId}")]
    public async Task<IActionResult> DeleteNotification(string notificationId)
    {
        try
        {
            var result = await _notificationService.DeleteNotificationAsync(notificationId);
            if (!result)
            {
                _logger.LogWarning("Notification with ID {id} not found.", notificationId);
                return NotFound(new { message = $"Notification with ID {notificationId} not found." });
            }
            _logger.LogInformation("Deleted notification with ID {id}.", notificationId);
            return Ok(result);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError("Invalid input: {message}", ex.Message);
            return BadRequest(new { message = $"Invalid input: {ex.Message}" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Internal server error: {message}", ex.Message);
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            _logger.LogError("An unexpected error occurred: {message}", ex.Message);
            return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
        }
    }



    /// <summary>
    /// Updates an existing notification.
    /// </summary>
    /// <param name="notificationDto">The notification data transfer object containing updated information.</param>
    /// <returns>An IActionResult indicating the result of the update operation.</returns>
    [HttpPut]
    public async Task<IActionResult> UpdateNotification([FromBody] NotificationDto.UpdateNotification notificationDto)
    {
        try
        {
            var result = await _notificationService.UpdateNotificationAsync(notificationDto);
            if (!result)
            {
                _logger.LogWarning("Notification with ID {notificationId} not found.", notificationDto.NotificationId);
                return NotFound(new { message = $"Notification with ID {notificationDto.NotificationId} not found." });
            }
            _logger.LogInformation("Notification updated successfully.");
            return Ok(new { message = "Notification updated successfully." });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError("Invalid input: {message}", ex.Message);
            return BadRequest(new { message = $"Invalid input: {ex.Message}" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Internal server error: {message}", ex.Message);
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            _logger.LogError("An unexpected error occurred: {message}", ex.Message);
            return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
        }
    }



    /// <summary>
    /// Retrieves unread notifications for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="language">The language for the notifications.</param>
    /// <returns>A list of unread notifications for the user.</returns>
    [HttpGet("user/{userId}/unread")]
    public async Task<IActionResult> GetUnreadNotifications(string userId, [FromQuery] string language = "en")
    {
        try
        {
            var notifications = await _notificationService.GetUnreadUserNotifications(userId, language);
            _logger.LogInformation("Retrieved unread notifications for user with ID {userId}.", userId);
            return Ok(notifications);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError("Invalid input: {message}", ex.Message);
            return BadRequest(new { message = $"Invalid input: {ex.Message}" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Internal server error: {message}", ex.Message);
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            _logger.LogError("An unexpected error occurred: {message}", ex.Message);
            return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
        }
    }


    /// <summary>
    /// Retrieves read notifications for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="language">The language for the notifications.</param>
    /// <returns>A list of read notifications for the user.</returns>
    [HttpGet("user/{userId}/read")]
    public async Task<IActionResult> GetReadNotifications(string userId, [FromQuery] string language = "en")
    {
        try
        {
            var notifications = await _notificationService.GetReadUserNotifications(userId, language);
            _logger.LogInformation("Retrieved read notifications for user with ID {userId}.", userId);
            return Ok(notifications);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError("Invalid input: {message}", ex.Message);
            return BadRequest(new { message = $"Invalid input: {ex.Message}" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Internal server error: {message}", ex.Message);
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            _logger.LogError("An unexpected error occurred: {message}", ex.Message);
            return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
        }
    }


    /// <summary>
    /// Retrieves notifications of a specific type for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="type">The notification type.</param>
    /// <param name="language">The language for the notifications.</param>
    /// <returns>A list of notifications of the specified type for the user.</returns>
    [HttpGet("user/{userId}/type/{type}")]
    public async Task<IActionResult> GetUserNotificationsByType(string userId, NotificationType type, [FromQuery] string language = "en")
    {
        try
        {
            var notifications = await _notificationService.GetUserNotificationsByType(userId, type, language);
            _logger.LogInformation("Retrieved notifications of type {type} for user with ID {userId}.", type, userId);
            return Ok(notifications);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError("Invalid input: {message}", ex.Message);
            return BadRequest(new { message = $"Invalid input: {ex.Message}" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Internal server error: {message}", ex.Message);
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            _logger.LogError("An unexpected error occurred: {message}", ex.Message);
            return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
        }
    }
    /// <summary>
    /// Retrieves the count of unread notifications for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The count of unread notifications for the user.</returns>
    [HttpGet("user/{userId}/unread/count")]
    public async Task<IActionResult> GetUnreadNotificationCount(string userId)
    {
        try
        {
            var unreadCount = await _notificationService.GetUnreadUserNotificationsCount(userId);
            _logger.LogInformation("Retrieved unread notification count for user with ID {userId}.", userId);
            return Ok(unreadCount);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError("Invalid input: {message}", ex.Message);
            return BadRequest(new { message = $"Invalid input: {ex.Message}" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Internal server error: {message}", ex.Message);
            return StatusCode(500, new { message = $"Internal error occurred: {ex.Message}" });
        }
        catch (Exception ex)
        {
            _logger.LogError("An unexpected error occurred: {message}", ex.Message);
            return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
        }
    }
}