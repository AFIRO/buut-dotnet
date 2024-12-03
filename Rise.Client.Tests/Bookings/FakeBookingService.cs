using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rise.Shared.Bookings;
using Rise.Shared.Enums;

namespace Xunit.Bookings;

public class FakeBookingService : IBookingService
{
    private IEnumerable<BookingDto.ViewBooking> _futureBooking = Enumerable.Range(1, 5).Select(i => new BookingDto.ViewBooking
    {
        bookingId = i.ToString(),
        bookingDate = DateTime.Now.AddDays(i),
        timeSlot = TimeSlot.Morning
    } );
    
    private IEnumerable<BookingDto.ViewBooking> _pastBookings = Enumerable.Range(1, 3).Select(i => new BookingDto.ViewBooking
    {
        bookingId = i.ToString(),
        bookingDate = DateTime.Now.Subtract(TimeSpan.FromDays(i)),
        timeSlot = TimeSlot.Morning
    } );
    
    private IEnumerable<BookingDto.ViewBookingCalender> _freeTimeslots = Enumerable.Range(1, 3).Select(i => new BookingDto.ViewBookingCalender
    {
        BookingDate = DateTime.Now.AddDays(i),
        Available = true
    } );
    
    public Task<IEnumerable<BookingDto.ViewBooking>?> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task<BookingDto.ViewBooking?> GetBookingById(string id)
    {
        throw new NotImplementedException();
    }

    public Task<BookingDto.ViewBooking> CreateBookingAsync(BookingDto.NewBooking booking)
    {
        throw new NotImplementedException();
    }

    public Task<Boolean> UpdateBookingAsync(BookingDto.UpdateBooking booking)
    {
        throw new NotImplementedException();
    }

    public Task<Boolean> DeleteBookingAsync(string id)
    {
        var tempList = _futureBooking.ToList();
        tempList.Remove(tempList.First(x => x.bookingId == id));
        
        _futureBooking = tempList;

        return Task.FromResult(true);
    }

    public Task<IEnumerable<BookingDto.ViewBooking>?> GetAllUserBookings(string userId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<BookingDto.ViewBooking>?> GetFutureUserBookings(string userId)
    {
        return Task.FromResult(_futureBooking);
    }

    public Task<IEnumerable<BookingDto.ViewBooking>?> GetPastUserBookings(string userId)
    {
        
        return Task.FromResult(_pastBookings);
    }

    public Task<IEnumerable<BookingDto.ViewBookingCalender>?> GetTakenTimeslotsInDateRange(DateTime? startDate, DateTime? endDate)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<BookingDto.ViewBookingCalender>?> GetFreeTimeslotsInDateRange(DateTime? startDate, DateTime? endDate)
    {
        return Task.FromResult(_freeTimeslots);
    }

    public Task<int> GetAmountOfFreeTimeslotsForWeek()
    {
        throw new NotImplementedException();
    }

    public Task<BookingDto.ViewBookingCalender> GetFirstFreeTimeSlot()
    {
        throw new NotImplementedException();
    }
}