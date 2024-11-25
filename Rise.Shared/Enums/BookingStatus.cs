using Rise.Shared.Bookings;

namespace Rise.Shared.Enums;

public enum BookingStatus
{
    /// <summary>
    /// The Booking is open for modification
    /// </summary>
    OPEN,
    /// <summary>
    /// The Booking is closed for modification
    /// </summary>
    CLOSED,    
    /// <summary>
    /// The Booking is completed
    /// </summary>
    COMPLETED,
    /// <summary>
    /// The Booking is canceled
    /// </summary>
    CANCELED,
    /// <summary>
    /// The Booking is refunded
    /// </summary>
    REFUNDED
}

public static class BookingStatusHelper
{
    public static BookingStatus GetBookingStatus(bool deleted, bool refunded, DateTime bookingDate, bool boatAssigned)
    {
        if (deleted)
            return BookingStatus.CANCELED;

        if (refunded)
            return BookingStatus.REFUNDED;
        
        if (boatAssigned && bookingDate > DateTime.Today)
            return BookingStatus.CLOSED;
        
        if (!boatAssigned && bookingDate > DateTime.Today)
            return BookingStatus.OPEN;
        
        return BookingStatus.COMPLETED;
    }
}