using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Rise.Client.Components.Table;
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
    private bool _isLoading = false;
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
        _isLoading = true;
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
        _isLoading = false;
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

    private List<TableHeader> Headers => new List<TableHeader>
    {
        new(Localizer["Title"]),
        new(Localizer["Message"], "d-none d-md-table-cell"),
        new(Localizer["Sent"])
    };

    private List<List<RenderFragment>> Data(IEnumerable<NotificationDto.ViewNotification> notifications)
    {
        var data = new List<List<RenderFragment>>();
        foreach (var notification in notifications)
        {
            var row = new List<RenderFragment>
            {
            TableCellService.NotificationTitleCell(notification.Title, notification.IsRead,EventCallback.Factory.Create(this, () => HandleNotificationClick(notification.NotificationId, notification.IsRead))),
            TableCellService.UserCell(notification.Message, "d-none d-md-table-cell"),
            TableCellService.BadgeCell(notification.Type.ToString()),
            TableCellService.DateAndTimeCell(notification.CreatedAt.ToShortDateString(),notification.CreatedAt.ToShortTimeString()),
            };

            data.Add(row);
        }
        return data;
    }
}