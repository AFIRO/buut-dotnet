using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rise.Domain.Bookings;
using Rise.Persistence;
using Rise.Server.Settings;
using Rise.Shared.Bookings;

namespace Rise.Services.Bookings;

/// <summary>
/// Service for allocating bookings.
/// </summary>
public class BookingAllocationService
{
    private readonly BookingAllocator _bookingAllocator;
    private readonly int _minReservationDays;
    private readonly int _maxReservationDays;
    private readonly ApplicationDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="BookingAllocationService"/> class.
    /// </summary>
    /// <param name="bookingAllocator">The booking allocator.</param>
    /// <param name="dbContext">The database context.</param>
    /// <param name="options">The booking settings options.</param>
    public BookingAllocationService(BookingAllocator bookingAllocator, ApplicationDbContext dbContext,
        IOptions<BookingSettings> options)
    {
        _bookingAllocator = bookingAllocator;
        _minReservationDays = options.Value.MinReservationDays;
        _maxReservationDays = options.Value.MaxReservationDays;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Allocates daily bookings for a specified date.
    /// </summary>
    /// <param name="date">The date for which to allocate bookings.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task AllocateDailyBookingAsync(DateTime date)
    {
        var bookings = await _dbContext.Bookings.Where(x => x.BookingDate.Date <= date.Date && x.BookingDate.Date >= DateTime.Today.Date && x.Battery == null && x.Boat == null).ToListAsync();
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