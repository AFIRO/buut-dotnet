using Rise.Shared.Enums;
using Rise.Shared.Notifications;
namespace Rise.Client.Notifications;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

public class NotificationService : INotificationService
{

    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public NotificationService(HttpClient _httpClient)
    {
        this._httpClient = _httpClient;
        this._jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        this._jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public Task<NotificationDto.ViewNotification> CreateNotificationAsync(NotificationDto.NewNotification notification, string language = "en")
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteNotificationAsync(string id)
    {
        throw new NotImplementedException();
    }

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


    public Task<NotificationDto.ViewNotification?> GetNotificationById(string id, string language = "en")
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<NotificationDto.ViewNotification>?> GetReadUserNotifications(string userId, string language = "en")
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<NotificationDto.ViewNotification>?> GetUnreadUserNotifications(string userId, string language = "en")
    {
        throw new NotImplementedException();
    }

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