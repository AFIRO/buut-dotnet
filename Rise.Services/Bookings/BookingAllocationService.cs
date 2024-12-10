using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rise.Domain.Bookings;
using Rise.Persistence;
using Rise.Server.Settings;

namespace Rise.Services.Bookings;

/// <summary>
/// Service for allocating bookings.
/// </summary>
public class BookingAllocationService
{
    private readonly BookingAllocator _bookingAllocator;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<BookingAllocationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BookingAllocationService"/> class.
    /// </summary>
    /// <param name="bookingAllocator">The booking allocator.</param>
    /// <param name="dbContext">The database context.</param>
    /// <param name="options">The booking settings options.</param>
    /// <param name="logger">The logger instance.</param>
    public BookingAllocationService(BookingAllocator bookingAllocator, ApplicationDbContext dbContext,
        IOptions<BookingSettings> options, ILogger<BookingAllocationService> logger)
    {
        _bookingAllocator = bookingAllocator;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Allocates daily bookings for a specified date.
    /// </summary>
    /// <param name="date">The date for which to allocate bookings.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task AllocateDailyBookingAsync(DateTime date)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error allocating daily bookings.");
            throw;
        }
    }
}