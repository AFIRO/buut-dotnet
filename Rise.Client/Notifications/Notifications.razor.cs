using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Rise.Shared.Notifications;

namespace Rise.Client.Notifications;

/// <summary>
/// Represents the notifications component.
/// </summary>
public partial class Notifications
{
    private IEnumerable<NotificationDto.ViewNotification>? notifications;
    private NotificationDto.NotificationCount? notificationCount;
    private string? userIdAuth0;
    private string language = "en";

    /// <summary>
    /// Gets or sets the notification service.
    /// </summary>
    [Inject] public required INotificationService NotificationService { get; set; }
    /// <summary>
    /// Gets or sets the notification state service.
    /// </summary>
    [Inject] public required NotificationStateService NotificationState { get; set; }
    /// <summary>
    /// Gets or sets the authentication state provider.
    /// </summary>
    [Inject] public required AuthenticationStateProvider AuthenticationStateProvider { get; set; }

    [Inject] private IJSRuntime Js { get; set; } = default!;


    /// <summary>
    /// Initializes the component and loads the user's notifications.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        language = await Js.InvokeAsync<string>("blazorCulture.get");
        // Get the current user's authentication state
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        userIdAuth0 = authState.User.Claims.FirstOrDefault(c => c.Type == "sub").Value;
        if (!string.IsNullOrEmpty(userIdAuth0))
        {
            notifications = await NotificationService.GetAllUserNotifications(userIdAuth0, language);
            notificationCount = await NotificationService.GetUnreadUserNotificationsCount(userIdAuth0);
            NotificationState.UpdateNotificationCount(notificationCount.Count);
        }
    }

    private async void HandleNotificationClick(string NotificationId, bool IsRead)
    {
        NotificationDto.UpdateNotification updateNotification = new NotificationDto.UpdateNotification
        {
            NotificationId = NotificationId,
            IsRead = !IsRead
        };

        var response = await NotificationService.UpdateNotificationAsync(updateNotification);
        if (response)
        {
            string language = await Js.InvokeAsync<string>("blazorCulture.get");
            notifications = await NotificationService.GetAllUserNotifications(userIdAuth0!, language);

            // Refresh unread count and notify NavBar
            var count = await NotificationService.GetUnreadUserNotificationsCount(userIdAuth0!);
            NotificationState.UpdateNotificationCount(count.Count);

            StateHasChanged();
        }
    }

}