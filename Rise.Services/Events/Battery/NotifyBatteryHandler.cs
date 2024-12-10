using Microsoft.Extensions.Logging;
using Rise.Shared.Batteries;
using Rise.Shared.Enums;
using Rise.Shared.Notifications;
using Rise.Shared.Users;

namespace Rise.Services.Events.Battery;

/// <summary>
/// Handles events when a battery is held too long by a user.
/// </summary>
public class NotifyOnBatteryTooLongWithUserEventHandler : IEventHandler<BatteryTooLongWithUserEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IBatteryService _batteryService;
    private readonly ILogger<NotifyOnBatteryTooLongWithUserEventHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyOnBatteryTooLongWithUserEventHandler"/> class.
    /// </summary>
    /// <param name="notificationService">The notification service.</param>
    /// <param name="userService">The user service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="batteryService">The battery service.</param>
    public NotifyOnBatteryTooLongWithUserEventHandler(
        INotificationService notificationService,
        ILogger<NotifyOnBatteryTooLongWithUserEventHandler> logger,
        IBatteryService batteryService)
    {
        _notificationService = notificationService;
        _logger = logger;
        _batteryService = batteryService ?? throw new ArgumentNullException(nameof(batteryService));
    }
    /// <summary>
    /// Handles the BatteryTooLongWithUserEvent asynchronously.
    /// </summary>
    /// <param name="event">The event data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <summary>
    /// Handles the BatteryTooLongWithUserEvent asynchronously.
    /// </summary>
    /// <param name="event">The event data.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task HandleAsync(BatteryTooLongWithUserEvent @event)
    {
        try
        {
            await NotifyUser(@event);
            await NotifyBuutAgent(@event);
        } catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while handling the BatteryTooLongWithUserEvent for Battery ID: {BatteryId}", @event.BatteryId);
            throw; // Rethrow to allow upstream handling if necessary
        }
    }

    private async Task NotifyBuutAgent(BatteryTooLongWithUserEvent @event)
    {
        var notification = new NotificationDto.NewNotification
        {
            UserId = @event.BuutAgentId,
            Title_EN = "Battery Held Too Long",
            Title_NL = "Batterij te lang vastgehouden",
            Message_EN = "The current holder of a battery has held it too long. Please contact them to arrange a pickup.",
            Message_NL = "De huidige houder van een batterij heeft deze te lang vastgehouden. Neem contact met hen op om een afspraak te maken.",
            RelatedEntityId = @event.BatteryId,
            Type = NotificationType.Battery
        };

        await CreateNotificationAsync(notification, "BuutAgent", @event.BuutAgentId);
    }

    private async Task NotifyUser(BatteryTooLongWithUserEvent @event)
    {
        var notification = new NotificationDto.NewNotification
        {
            UserId = @event.UserId,
            Title_EN = "Battery Held Too Long",
            Title_NL = "Batterij te lang vastgehouden",
            Message_EN = "You have held the battery very long in your possesion. The buutagent will contact you to pick it up.",
            Message_NL = "U heeft de batterij erg lang in uw bezit gehad. De buutagent zal u contacteren om deze op te halen.",
            RelatedEntityId = @event.BatteryId,
            Type = NotificationType.Battery
        };

        await CreateNotificationAsync(notification, "User", @event.UserId);
    }

    private async Task CreateNotificationAsync(NotificationDto.NewNotification notification, string recipientType, string? recipientId = null)
    {
        try
        {
            await _notificationService.CreateNotificationAsync(notification);
            _logger.LogInformation("Notification created for {RecipientType} with ID: {RecipientId}", recipientType, recipientId ?? notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification for {RecipientType} with ID: {RecipientId}", recipientType, recipientId ?? notification.UserId);
            throw;
        }
    }
}