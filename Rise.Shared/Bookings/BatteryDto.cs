using Rise.Shared.Batteries;
using Rise.Shared.Users;

namespace Rise.Shared.Bookings;

public class BatteryDto
{
    public class NewBattery
    {
        public string? name { get; set; } = default!;
    }

    public class ViewBattery
    {
        public string name { get; set; } = default!;
        public int countBookings { get; set; } = default!;
        public List<string> listComments = default!;
    }

    public class ViewBatteryBuutAgent
    {
        public string id { get; set; } = default!;
        public string name { get; set; } = default!;
        public int countBookings { get; set; } = default!;
        public List<string> listComments = default!;
    }
    
    
    public class ViewBatteryWithCurrentUser : ViewBattery
    {
        public UserDto.ContactUser? currentUser { get; set; } = default!;
    }
}