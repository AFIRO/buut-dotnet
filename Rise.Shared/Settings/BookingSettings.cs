namespace Rise.Server.Settings;

/// <summary>
/// Global settings for bookings.
/// </summary>
public class BookingSettings
{
    /// <summary>
    /// Max amount of bookings a user can have.
    /// </summary>
    public int MaxBookingLimit { get; set; }

    /// <summary>
    /// Minimum days before reservation day you can make a new reservation.
    /// </summary>
    public int MinReservationDays { get; set; }

    /// <summary>
    /// Maximum days before reservation day you can make a new reservation.
    /// </summary>
    public int MaxReservationDays { get; set; }
}