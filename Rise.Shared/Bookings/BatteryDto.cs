using Rise.Shared.Batteries;
using Rise.Shared.Users;

namespace Rise.Shared.Bookings;

public class BatteryDto
{
    public class NewBattery
    {
        public string? name { get; set; } = default!;
    }

    /// <summary>
    /// Represents a view model for a battery.
    /// </summary>
    public class ViewBattery
    {
        /// <summary>
        /// Gets or sets the name of the battery.
        /// </summary>
        public string name { get; set; } = default!;
        /// <summary>
        /// Gets or sets the amount the battery is booked.
        /// </summary>
        public int countBookings { get; set; } = default!;
        /// <summary>
        /// Gets or sets a list of comments about the battery.
        /// </summary>
        public List<string> listComments = default!;
    }


    /// <summary>
    /// Represents a view model for a battery information as needed by the Buutagent.
    /// </summary>
    public class ViewBatteryBuutAgent
    {
        /// <summary>
        /// Gets or sets the ID of the battery.
        /// </summary>
        public string id { get; set; } = default!;
        /// <summary>
        /// Gets or sets the name of the battery.
        /// </summary>
        public string name { get; set; } = default!;
        /// <summary>
        /// Gets or sets the amount the battery is booked.
        /// </summary>
        public int countBookings { get; set; } = default!;
        /// <summary>
        /// Gets or sets a list of comments about the battery.
        /// </summary>
        public List<string> listComments = default!;
    }
    
    
    public class ViewBatteryWithCurrentUser : ViewBattery
    {
        public UserDto.ContactUser? currentUser { get; set; } = default!;
    }
}