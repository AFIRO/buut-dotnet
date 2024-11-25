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

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationController"/> class.
    /// </summary>
    /// <param name="notificationService">The notification service.</param>
    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Retrieves all notifications.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllNotifications([FromQuery] String language = "en")
    {
        try
        {
            var notifications = await _notificationService.GetAllNotificationsAsync(language);
            return Ok(notifications);
        }
        catch (ArgumentNullException ex)
        {
            return BadRequest(new { message = $"Invalid input: {ex.Message}" });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves all notifications for a specific user.
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserNotifications(String userId, [FromQuery] String language = "en")
    {
        try
        {
            var notifications = await _notificationService.GetAllUserNotifications(userId, language);
            return Ok(notifications);
        }
        catch (ArgumentNullException ex)
        {
            return BadRequest(new { message = $"Invalid input: {ex.Message}" });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new notification.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateNotification([FromBody] NotificationDto.NewNotification notification, [FromQuery] String language = "en")
    {
        try
        {
            var createdNotification = await _notificationService.CreateNotificationAsync(notification, language);
            return Ok(createdNotification);
        }
        catch (ArgumentNullException ex)
        {
            return BadRequest(new { message = $"Invalid input: {ex.Message}" });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a notification by its ID.
    /// </summary>
    [HttpGet("{notificationId}")]
    public async Task<IActionResult> GetNotificationById(String notificationId, [FromQuery] String language = "en")
    {
        try
        {
            var notification = await _notificationService.GetNotificationById(notificationId, language);
            if (notification == null)
            {
                return NotFound(new { message = $"Notification with ID {notificationId} not found." });
            }
            return Ok(notification);
        }
        catch (ArgumentNullException ex)
        {
            return BadRequest(new { message = $"Invalid input: {ex.Message}" });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
        }
    }
    /// <summary>
    /// Deletes a notification by its ID.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(String id)
    {
        try
        {
            var result = await _notificationService.DeleteNotificationAsync(id);
            if (!result)
            {
                return NotFound(new { message = $"Notification with ID {id} not found." });
            }
            return Ok(result);
        }
        catch (ArgumentNullException ex)
        {
            return BadRequest(new { message = $"Invalid input: {ex.Message}" });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing notification.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdateNotification([FromBody] NotificationDto.UpdateNotification notificationDto)
    {
        try
        {
            var result = await _notificationService.UpdateNotificationAsync(notificationDto);
            if (!result)
            {
                return NotFound(new { message = $"Notification with ID {notificationDto.NotificationId} not found." });
            }
            return Ok(new { message = "Notification updated successfully." });
        }
        catch (ArgumentNullException ex)
        {
            return BadRequest(new { message = $"Invalid input: {ex.Message}" });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves unread notifications for a specific user.
    /// </summary>
    [HttpGet("user/{userId}/unread")]
    public async Task<IActionResult> GetUnreadNotifications(String userId, [FromQuery] String language = "en")
    {
        try
        {
            var notifications = await _notificationService.GetUnreadUserNotifications(userId, language);
            return Ok(notifications);
        }
        catch (ArgumentNullException ex)
        {
            return BadRequest(new { message = $"Invalid input: {ex.Message}" });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves read notifications for a specific user.
    /// </summary>
    [HttpGet("user/{userId}/read")]
    public async Task<IActionResult> GetReadNotifications(String userId, [FromQuery] String language = "en")
    {
        try
        {
            var notifications = await _notificationService.GetReadUserNotifications(userId, language);
            return Ok(notifications);
        }
        catch (ArgumentNullException ex)
        {
            return BadRequest(new { message = $"Invalid input: {ex.Message}" });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves notifications of a specific type for a specific user.
    /// </summary>
    [HttpGet("user/{userId}/type/{type}")]
    public async Task<IActionResult> GetUserNotificationsByType(String userId, NotificationType type, [FromQuery] String language = "en")
    {
        try
        {
            var notifications = await _notificationService.GetUserNotificationsByType(userId, type, language);
            return Ok(notifications);
        }
        catch (ArgumentNullException ex)
        {
            return BadRequest(new { message = $"Invalid input: {ex.Message}" });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
        }
    }
    [HttpGet("user/{userId}/unread/count")]
    public async Task<IActionResult> GetUnreadNotificationCount(String userId)
    {
        try
        {
            var unreadCount = await _notificationService.GetUnreadUserNotificationsCount(userId);
            return Ok(unreadCount);
        }
        catch (ArgumentNullException ex)
        {
            return BadRequest(new { message = $"Invalid input: {ex.Message}" });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { message = $"Internal error occurred: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
        }
    }
}