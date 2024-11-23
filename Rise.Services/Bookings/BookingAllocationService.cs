using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rise.Domain.Bookings;
using Rise.Persistence;
using Rise.Server.Settings;
using Rise.Shared.Bookings;

namespace Rise.Services.Bookings;

public class BookingAllocationService
{
    private readonly BookingAllocator _bookingAllocator;
    private readonly int _minReservationDays;
    private readonly int _maxReservationDays;
    private readonly ApplicationDbContext _dbContext;


    public BookingAllocationService(BookingAllocator bookingAllocator, ApplicationDbContext dbContext,
        IOptions<BookingSettings> options)
    {
        _bookingAllocator = bookingAllocator;
        _minReservationDays = options.Value.MinReservationDays;
        _maxReservationDays = options.Value.MaxReservationDays;
        _dbContext = dbContext;
    }

    public async Task AllocateDailyBookingAsync(DateTime date)
    {
        var bookings = await _dbContext.Bookings.Where(x => x.BookingDate.Date <= date.Date && x.BookingDate.Date >=DateTime.Today.Date && x.Battery == null && x.Boat == null).ToListAsync();
        var batteries = await _dbContext.Batteries.ToListAsync();
        var boats = await _dbContext.Boats.ToListAsync();
        
        _bookingAllocator.assignBatteriesBoats(bookings, batteries, boats, date);

        foreach (var booking in bookings)
        {
            _dbContext.Bookings.Update(booking);

        }
        
        await _dbContext.SaveChangesAsync();
    }
}