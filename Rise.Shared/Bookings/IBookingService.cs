namespace Rise.Shared.Bookings;

public interface IBookingService
{
    Task<IEnumerable<BookingDto.ViewBooking>?> GetAllAsync();
    Task<BookingDto.ViewBooking?> GetBookingById(string id);
    Task<BookingDto.ViewBooking> CreateBookingAsync(BookingDto.NewBooking booking);
    Task<bool> UpdateBookingAsync(BookingDto.UpdateBooking booking);
    Task<bool> DeleteBookingAsync(string id);
    
    Task<IEnumerable<BookingDto.ViewBooking>?> GetAllUserBookings(string userId);
    
    Task<IEnumerable<BookingDto.ViewBooking>?> GetFutureUserBookings(string userId);
    Task<IEnumerable<BookingDto.ViewBooking>?> GetPastUserBookings(string userId);

    Task<IEnumerable<BookingDto.ViewBookingCalender>?> GetTakenTimeslotsInDateRange(DateTime? startDate, DateTime? endDate);
    Task<IEnumerable<BookingDto.ViewBookingCalender>?> GetFreeTimeslotsInDateRange(DateTime? startDate, DateTime? endDate);
    
    Task<int> GetAmountOfFreeTimeslotsForWeek();
    Task<BookingDto.ViewBookingCalender> GetFirstFreeTimeSlot();

}