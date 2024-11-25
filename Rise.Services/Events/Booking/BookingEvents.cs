using Rise.Shared.Enums;

namespace Rise.Services.Events.Booking;

/// <summary>
/// Event triggered when a booking is created.
/// </summary>
/// <param name="bookingId">The booking ID.</param>
/// <param name="userId">The user ID.</param>
/// <param name="bookingDate">The booking date.</param>
/// <param name="timeSlot">The time slot.</param>
public class BookingCreatedEvent(string bookingId, string userId, DateTime bookingDate, TimeSlot timeSlot) : IEvent
{
    /// <summary>
    /// Gets the booking ID.
    /// </summary>
    public string BookingId { get; } = bookingId;

    /// <summary>
    /// Gets the user ID.
    /// </summary>
    public string UserId { get; } = userId;

    /// <summary>
    /// Gets the booking date.
    /// </summary>
    public DateTime BookingDate { get; } = bookingDate;

    /// <summary>
    /// Gets the time slot.
    /// </summary>
    public TimeSlot TimeSlot { get; } = timeSlot;
}

/// <summary>
/// Event triggered when a booking is deleted.
/// </summary>
/// <param name="bookingId">The booking ID.</param>
/// <param name="userId">The user ID.</param>
/// <param name="bookingDate">The booking date.</param>
/// <param name="timeSlot">The time slot.</param>
public class BookingDeletedEvent(string bookingId, string userId, DateTime bookingDate, TimeSlot timeSlot) : IEvent
{
    /// <summary>
    /// Gets the booking ID.
    /// </summary>
    public string BookingId { get; } = bookingId;

    /// <summary>
    /// Gets the user ID.
    /// </summary>
    public string UserId { get; } = userId;

    /// <summary>
    /// Gets the booking date.
    /// </summary>
    public DateTime BookingDate { get; } = bookingDate;

    /// <summary>
    /// Gets the time slot.
    /// </summary>
    public TimeSlot TimeSlot { get; } = timeSlot;
}

/// <summary>
/// Event triggered when a booking is updated.
/// </summary>
/// <param name="bookingId">The booking ID.</param>
/// <param name="userId">The user ID.</param>
/// <param name="oldBookingDate">The old booking date.</param>
/// <param name="oldTimeSlot">The old time slot.</param>
/// <param name="newBookingDate">The new booking date.</param>
/// <param name="newTimeSlot">The new time slot.</param>
public class BookingUpdatedEvent(string bookingId, string userId, DateTime oldBookingDate, TimeSlot oldTimeSlot, DateTime newBookingDate, TimeSlot newTimeSlot) : IEvent
{
    /// <summary>
    /// Gets the booking ID.
    /// </summary>
    public string BookingId { get; } = bookingId;

    /// <summary>
    /// Gets the user ID.
    /// </summary>
    public string UserId { get; } = userId;

    /// <summary>
    /// Gets the old booking date.
    /// </summary>
    public DateTime OldBookingDate { get; } = oldBookingDate;

    /// <summary>
    /// Gets the old time slot.
    /// </summary>
    public TimeSlot OldTimeSlot { get; } = oldTimeSlot;

    /// <summary>
    /// Gets the new booking date.
    /// </summary>
    public DateTime NewBookingDate { get; } = newBookingDate;

    /// <summary>
    /// Gets the new time slot.
    /// </summary>
    public TimeSlot NewTimeSlot { get; } = newTimeSlot;
}
