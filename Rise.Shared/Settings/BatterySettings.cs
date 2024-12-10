namespace Rise.Server.Settings;

/// <summary>
/// Represents the settings for managing battery-related operations.
/// </summary>
public class BatterySettings
{
    /// <summary>
    /// Gets or sets the number of days a battery can be held before triggering a warning for the BUUT agent to retrieve it.
    /// </summary>
    public int HoldWarningThresholdDays { get; set; }

    /// <summary>
    /// Gets or sets the interval, in days, for sending additional warnings after the initial hold warning.
    /// </summary>
    public int RewarningIntervalDays { get; set; }

    /// <summary>
    /// Gets or sets the number of sailings a battery can undergo before requiring maintenance.
    /// </summary>
    public int MaintenanceIntervalSailings { get; set; }
}
