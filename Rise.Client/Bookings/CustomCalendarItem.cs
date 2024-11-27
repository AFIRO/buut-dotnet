using Heron.MudCalendar;
using Rise.Shared.Enums;

namespace Rise.Client.Bookings;


public class CustomCalenderItem : CalendarItem
{
    // Map TimeSlot to Color
    private static readonly Dictionary<TimeSlot, string> TimeSlotColors = new()
    {
        { TimeSlot.Morning, "#FF00696D" },
        { TimeSlot.Afternoon, "#FF4A6364" },
        { TimeSlot.Evening, "#FF4E5F7D" },
        { TimeSlot.None, "White" } // Default color for unavailable or undefined slots
    };

    private string _color = TimeSlotColors[TimeSlot.None];
    private bool _available;

        
    public string Color
    {
        get => _color;
        set => _color = value;
    }

    public bool Available
    {
        get => _available;
        set
        {
            if (value)
            {
                // Determine TimeSlot based on Start.Hour and set color
                var timeSlot = TimeSlotEnumExtensions.ToTimeSlot(Start.Hour);
                Color = TimeSlotColors.TryGetValue(timeSlot, out var color) ? color : TimeSlotColors[TimeSlot.None];
            }
            else
            {
                Color = TimeSlotColors[TimeSlot.None];
            }

            _available = value;
        }
    }
}
