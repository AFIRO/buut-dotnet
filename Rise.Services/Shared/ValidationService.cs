using Rise.Shared.Bookings;
using Rise.Shared.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rise.Persistence;
using Rise.Server.Settings;

namespace Rise.Shared.Services;

/// <summary>
/// Service for validating various entities and operations.
/// </summary>
public class ValidationService : IValidationService
{
    private readonly int _maxBookingLimit;
    private readonly ApplicationDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationService"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="options">The booking settings options.</param>
    public ValidationService(ApplicationDbContext dbContext, IOptions<BookingSettings> options)
    {
        _maxBookingLimit = options.Value.MaxBookingLimit;
        this._dbContext = dbContext;
    }

    /// <summary>
    /// Checks if a user exists asynchronously.
    /// </summary>
    /// <param name="userId">The ID of the user to check.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the user exists.</returns>
    public async Task<bool> CheckUserExistsAsync(string userId)
    {
        // Check if user exists in the database
        return await _dbContext.Users
            .AnyAsync(u => u.IsDeleted == false && u.Id == userId);
    }

    /// <summary>
    /// Checks if a booking exists for a specific date asynchronously.
    /// </summary>
    /// <param name="bookingDate">The date of the booking to check.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the booking exists.</returns>
    public async Task<bool> BookingExists(DateTime bookingDate)
    {
        return await _dbContext.Bookings
            .AnyAsync(x => x.IsDeleted == false && x.BookingDate == bookingDate);
    }

    /// <summary>
    /// Checks if a user has reached the maximum booking limit asynchronously.
    /// </summary>
    /// <param name="userId">The ID of the user to check.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the user has reached the maximum booking limit.</returns>
    public async Task<bool> CheckUserMaxBookings(string userId)
    {
        // Returns true if the user has reached its limit
        return await _dbContext.Bookings.Where(x => x.IsDeleted == false
                                                    && x.UserId == userId
                                                    && x.BookingDate >= DateTime.Today).CountAsync() >=
               _maxBookingLimit;
    }

    /// <summary>
    /// Validates a booking asynchronously.
    /// </summary>
    /// <param name="userId">The ID of the user making the booking.</param>
    /// <param name="booking">The booking details to validate.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the booking is valid.</returns>
    /// <exception cref="ArgumentException">Thrown when the user does not exist.</exception>
    public async Task<bool> ValidateBookingAsync(string userId, BookingDto.UpdateBooking booking)
    {
        var userExists = await CheckUserExistsAsync(userId);
        if (!userExists)
        {
            throw new ArgumentException("User does not exist.");
        }

        // Further booking validation logic (e.g., check if a time slot is available)
        return true;
    }

    /// <summary>
    /// Checks if a user has active bookings asynchronously.
    /// </summary>
    /// <param name="userId">The ID of the user to check.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the user has active bookings.</returns>
    /// <exception cref="ArgumentException">Thrown when the user does not exist.</exception>
    public async Task<bool> CheckActiveBookings(string userId)
    {
        var userExists = await CheckUserExistsAsync(userId);
        if (!userExists)
        {
            throw new ArgumentException("User does not exist.");
        }
        
        return await _dbContext.Bookings.Where(x => x.IsDeleted == false && x.UserId == userId).CountAsync() > 0;
    }

    /// <summary>
    /// Checks if a boat with the specified name exists asynchronously.
    /// </summary>
    /// <param name="boatName">The name of the boat to check.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the boat exists.</returns>
    public async Task<bool> BoatExists(string boatName)
    {
        return await _dbContext.Boats.AnyAsync(boat => !boat.IsDeleted && boat.Name == boatName);
    }

    /// <summary>
    /// Checks if a battery with the specified name exists asynchronously.
    /// </summary>
    /// <param name="name">The name of the battery to check.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the battery exists.</returns>
    public async Task<bool> BatteryExists(string name)
    {
        return await _dbContext.Batteries.AnyAsync(battery => !battery.IsDeleted && battery.Name == name);
    }
}