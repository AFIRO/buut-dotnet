namespace Rise.Domain.Bookings;

using System.ComponentModel.DataAnnotations;
using Rise.Shared.Enums;

/// <summary>
/// Represents a booking entity in the system
/// </summary>
public class Booking : Entity
{
    #region Fields

    private string _id = Guid.NewGuid().ToString();

    private DateTime _bookingDate;
    private Boat? _boat;
    private Battery? _battery;
    private string _userId; // Foreign key referencing User

    #endregion

    #region Constructors

    /// <summary>
    ///Private constructor for Entity Framework Core
    /// </summary>
    private Booking()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Booking"/> class with the specified details.
    /// </summary>
    /// <param name="bookingDate">The date of the booking.</param>
    /// <param name="userId">The ID of the user making the booking.</param>
    public Booking(DateTime bookingDate, string userId)
    {
        BookingDate = bookingDate;
        UserId = userId;
    }
    
    public Booking(DateTime bookingDate, string userId, TimeSlot timeSlot)
    {
        BookingDate = bookingDate;
        UserId = userId;
        AddTimeSlot(timeSlot);
    }

    #endregion


    #region Properties

    public string Id
    {
        get => _id;
        set => _id = Guard.Against.NullOrWhiteSpace(value, nameof(Id));
    }
    
    public string UserId
    {
        get => _userId;
        set => _userId = Guard.Against.Null(value);
    }
    

    /// <summary>
    /// Gets or sets the booking date.
    /// </summary>
    public DateTime BookingDate
    {
        get => _bookingDate;
        set => _bookingDate = Guard.Against.Default(value, nameof(BookingDate));
    }
    
    public string? BoatId { get; set; }

    /// <summary>
    /// Gets or sets the boat of the booking.
    /// </summary>
    public Boat Boat
    {
        get => _boat;
        set => _boat = value;
    }

    public string? BatteryId { get; set; }
    
    /// <summary>
    /// Gets or sets the battery of the booking.
    /// </summary>
    public Battery Battery
    {
        get => _battery;
        set => _battery = value;
    }

    #endregion


    #region Methods

    /// <summary>
    /// Adds a boat to the booking.
    /// </summary>
    /// <param name="boat">The boat to add.</param>
    public void AddBoat(Boat boat)
    {
        Guard.Against.Null(boat, nameof(boat));
        boat.AddBooking();
        Boat = boat;
    }

    /// <summary>
    /// Adds a battery to the booking.
    /// </summary>
    /// <param name="battery">The battery to add.</param>
    public void AddBattery(Battery battery)
    {
        Guard.Against.Null(battery, nameof(battery));
        battery.AddBooking();
        Battery = battery;
    }

    public void AddTimeSlot(TimeSlot timeSlot)
    {
        _bookingDate = _bookingDate.Date.AddHours(TimeSlotEnumExtensions.GetStartHour(timeSlot));
    }

    public TimeSlot GetTimeSlot()
    {
        return TimeSlotEnumExtensions.ToTimeSlot(_bookingDate.Hour);
    }

    #endregion
}