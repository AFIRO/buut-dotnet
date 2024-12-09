using Rise.Shared.Enums;
using Rise.Shared.Notifications;
namespace Rise.Client.Notifications;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Rise.Shared.Users;

/// <summary>
/// Service for handling notifications.
/// </summary>
public class NotificationService : INotificationService
{

    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationService"/> class.
    /// </summary>
    /// <param name="_httpClient">The HTTP client to be used for making requests.</param>
    public NotificationService(HttpClient _httpClient)
    {
        this._httpClient = _httpClient;
        this._jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
                  // Add the immutable converters for System.Collections.Immutable types
        this._jsonSerializerOptions.Converters.Add(new ImmutableListJsonConverter<RoleDto>());
        this._jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    /// <summary>
    /// Creates a new notification.
    /// </summary>
    /// <param name="notification">The notification to create.</param>
    /// <param name="language">The language for the notification.</param>
    /// <param name="sendEmail">Whether to send an email notification.</param>
    /// <returns>The created notification.</returns>
    public async Task<NotificationDto.ViewNotification> CreateNotificationAsync(NotificationDto.NewNotification notification, string language = "en", bool sendEmail = false)
    {
        var requestUri = $"notification?language={language}&sendEmail={sendEmail}";
        var response = await _httpClient.PostAsJsonAsync(requestUri, notification);

        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var createdNotification = JsonSerializer.Deserialize<NotificationDto.ViewNotification>(jsonResponse, _jsonSerializerOptions);
            return createdNotification ?? throw new InvalidOperationException("Failed to deserialize the created notification.");
        }
        else
        {
            throw new InvalidOperationException($"Failed to create notification. Status code: {response.StatusCode}");
        }
    }

    /// <summary>
    /// Creates a new notification and sends it to all users with the specified role.
    /// </summary>
    /// <param name="notification">The new notification data.</param>
    /// <param name="role">The role of the users to send the notification to.</param>
    /// <param name="language">The language for localization.</param>
    /// <param name="sendEmail">A boolean indicating whether to send an email notification.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task CreateAndSendNotificationToUsersByRoleAsync(NotificationDto.NewNotification notification, RolesEnum role, string language = "en", bool sendEmail = false)
    {
        var requestUri = $"notification/role/{role}?language={language}&sendEmail={sendEmail}";
        var response = await _httpClient.PostAsJsonAsync(requestUri, notification);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to create and send notification. Status code: {response.StatusCode}");
        }
    }

    /// <summary>
    /// Deletes a notification by its ID.
    /// </summary>
    /// <param name="id">The ID of the notification to delete.</param>
    /// <returns>A boolean indicating whether the deletion was successful.</returns>
    public Task<bool> DeleteNotificationAsync(string id)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Retrieves all notifications.
    /// </summary>
    /// <param name="language">The language for the notifications.</param>
    /// <returns>A collection of all notifications.</returns>
    public Task<IEnumerable<NotificationDto.ViewNotification>?> GetAllNotificationsAsync(string language = "en")
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Retrieves all notifications for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="language">The language for the notifications.</param>
    /// <returns>A collection of notifications for the user.</returns>
    public async Task<IEnumerable<NotificationDto.ViewNotification>?> GetAllUserNotifications(string userId, string language = "en")
    {
        try
        {
            // Send GET request
            var response = await _httpClient.GetAsync($"notification/user/{userId}?language={language}");

            // Check if the response was successful
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var notifications = JsonSerializer.Deserialize<IEnumerable<NotificationDto.ViewNotification>>(jsonResponse, _jsonSerializerOptions);
                return notifications ?? Enumerable.Empty<NotificationDto.ViewNotification>();
            }

            // Handle specific HTTP status codes
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new ArgumentException($"Bad Request: {errorMessage}");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null; // Return null if no notifications are found
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Server Error: {errorMessage}");
            }

            // Throw exception for other status codes
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"An error occurred while fetching notifications: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new Exception("An error occurred while processing the server response. Please check the data format.", ex);
        }

        // Return an empty collection if no notifications are found
        return Enumerable.Empty<NotificationDto.ViewNotification>();
    }


    /// <summary>
    /// Retrieves a notification by its ID.
    /// </summary>
    /// <param name="id">The ID of the notification to retrieve.</param>
    /// <param name="language">The language for the notification.</param>
    /// <returns>The notification with the specified ID.</returns>
    public Task<NotificationDto.ViewNotification?> GetNotificationById(string id, string language = "en")
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Retrieves read notifications for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="language">The language for the notifications.</param>
    /// <returns>A collection of read notifications for the user.</returns>
    public Task<IEnumerable<NotificationDto.ViewNotification>?> GetReadUserNotifications(string userId, string language = "en")
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Retrieves unread notifications for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="language">The language for the notifications.</param>
    /// <returns>A collection of unread notifications for the user.</returns>
    public Task<IEnumerable<NotificationDto.ViewNotification>?> GetUnreadUserNotifications(string userId, string language = "en")
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Retrieves notifications of a specific type for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="type">The type of notifications to retrieve.</param>
    /// <param name="language">The language for the notifications.</param>
    /// <returns>A collection of notifications of the specified type for the user.</returns>
    public Task<IEnumerable<NotificationDto.ViewNotification>?> GetUserNotificationsByType(string userId, NotificationType type, string language = "en")
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Updates an existing notification.
    /// </summary>
    /// <param name="notification">The notification to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    public async Task<bool> UpdateNotificationAsync(NotificationDto.UpdateNotification notification)
    {
        try{
            var response = await _httpClient.PutAsJsonAsync("notification", notification);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new Exception("Error updating notification", ex);
        }
        return false;
    }


    /// <summary>
    /// Retrieves the count of unread notifications for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>The count of unread notifications for the user.</returns>
    public async Task<NotificationDto.NotificationCount> GetUnreadUserNotificationsCount(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"notification/user/{userId}/unread/count");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NotificationDto.NotificationCount>();
                return result ?? new NotificationDto.NotificationCount();
            }

            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new Exception("Error fetching unread notification count", ex);
        }

        return new NotificationDto.NotificationCount();
    }

}