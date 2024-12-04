using System.Text.Json.Serialization;
using Rise.Shared.Boats;
using Rise.Shared.Enums;
using Rise.Shared.Users;

namespace Rise.Shared.Bookings;

public class BookingDto
{
    public class NewBooking
    {
        // public TimeSlot timeSlot { get; set; }
        public DateTime bookingDate { get; set; } = DateTime.Now;
        public string userId { get; set; } = default!;
    }

    public class ViewBooking
    {
        public string userId { get; set; } = default!;
        public string bookingId { get; set; } = default!;
        public DateTime bookingDate { get; set; } = DateTime.Now;
        public BoatDto.ViewBoat boat { get; set; } = new();
        public BookingStatus status { get; set; } = BookingStatus.COMPLETED;
        public BatteryDto.ViewBattery battery { get; set; } = new();


        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TimeSlot timeSlot { get; set; }
    }


    public class UpdateBooking
    {
        public string bookingId { get; set; } = default!;
        public DateTime? bookingDate { get; set; } = DateTime.Now;
        public BoatDto.NewBoat? boat { get; set; } = null;
        public BatteryDto.NewBattery? battery { get; set; } = null;
    }

    public class ViewBookingCalender
    {
        public DateTime BookingDate { get; set; } = DateTime.Now;
        public TimeSlot TimeSlot { get; set; }
        public bool Available { get; set; } = false;
    }
}