namespace Rise.Services.Events.Battery;

/// <summary>
/// Initializes a new instance of the <see cref="BatteryTooLongWithUserEvent"/> class.
/// </summary>
/// <param name="batteryId">The ID of the battery that has been held too long.</param>
/// <param name="userId">The ID of the user that has held the battery too long.</param>
/// <param name="buutAgentId">The ID of the buut agent responsible for the battery.</param>
public class BatteryTooLongWithUserEvent(string batteryId, string userId, string buutAgentId) : IEvent
{
    /// <summary>
    /// id of the battery that has been held too long
    /// </summary>
    public string BatteryId { get; } = batteryId;

    /// <summary>
    /// userId of the user that has held the battery too long
    /// </summary>
    public string UserId { get; } = userId;

    /// <summary>
    /// userId of the buutagetn responsible for the battery
    /// </summary>
    public string BuutAgentId { get; } = buutAgentId;
}