using Rise.Shared.Bookings;
using Rise.Shared.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rise.Persistence;
using Rise.Server.Settings;

namespace Rise.Shared.Services;

public class ValidationService : IValidationService
{
    private readonly int _maxBookingLimit;
    private readonly ApplicationDbContext _dbContext;

    public ValidationService(ApplicationDbContext dbContext, IOptions<BookingSettings> options)
    {
        _maxBookingLimit = options.Value.MaxBookingLimit;
        this._dbContext = dbContext;
    }

    public async Task<bool> CheckUserExistsAsync(string userId)
    {
        // Check if user exists in the database
        return await _dbContext.Users
            .AnyAsync(u => u.IsDeleted == false && u.Id == userId);
    }



    public async Task<bool> BookingExists(DateTime bookingDate)
    {
        return await _dbContext.Bookings
            .AnyAsync(x => x.IsDeleted == false && x.BookingDate == bookingDate);
    }

    public async Task<bool> CheckUserMaxBookings(string userId)
    {
        //returns true if the user has reached its limit
        return await _dbContext.Bookings.Where(x => x.IsDeleted == false
                                                    && x.UserId == userId
                                                    && x.BookingDate >= DateTime.Today).CountAsync() >=
               _maxBookingLimit;
    }

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

    public async Task<bool> CheckActiveBookings(string userId)
    {
        var userExists = await CheckUserExistsAsync(userId);
        if (!userExists)
        {
            throw new ArgumentException("User does not exist.");
        }
        
        return await _dbContext.Bookings.Where(x => x.IsDeleted == false && x.UserId == userId).CountAsync() > 0;
    }
    public async Task<bool> BoatExists(string boatName)
    {
        return await _dbContext.Boats.AnyAsync(boat => !boat.IsDeleted && boat.Name == boatName);
    }

    public async Task<bool> BatteryExists(string name)
    {
        return await _dbContext.Batteries.AnyAsync(battery => !battery.IsDeleted && battery.Name == name);
    }

}