using Rise.Shared.Bookings;

namespace Rise.Shared.Services;

public interface IValidationService
{
    Task<bool> CheckUserExistsAsync(string userId);
    Task<bool> BookingExists(DateTime bookingDate);
    Task<bool> CheckUserMaxBookings(string userId);
    Task<bool> ValidateBookingAsync(string userId, BookingDto.UpdateBooking booking);
}