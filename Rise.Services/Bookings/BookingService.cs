using System.Collections.Immutable;
using System.Text.Json;
using Auth0.ManagementApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Rise.Domain.Bookings;
using Rise.Persistence;
using Rise.Server.Settings;
using Rise.Shared.Bookings;
using Rise.Shared.Enums;
using Rise.Services.Users;
using Rise.Shared.Services;
using Rise.Shared.Users;
using Rise.Shared.Boats;

namespace Rise.Services.Bookings;

public class BookingService : IBookingService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly int _minReservationDays;
    private readonly int _maxReservationDays;
    private readonly IValidationService _validationService;


    public BookingService(ApplicationDbContext dbContext, IOptions<BookingSettings> options,
        IValidationService validationService)
    {
        _minReservationDays = options.Value.MinReservationDays;
        _maxReservationDays = options.Value.MaxReservationDays;
        _validationService = validationService;

        this._dbContext = dbContext;
        this._jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Retrieves all bookings.
    /// </summary>
    /// <returns>A collection of all bookings.</returns>
    public async Task<IEnumerable<BookingDto.ViewBooking>> GetAllAsync()
    {
        // Changed method so that DTO creation is out of the LINQ Query
        // You need to avoid using methods with optional parameters directly
        // in the LINQ query that EF is trying to translate
        var query = await _dbContext.Bookings
            .Include(b => b.Battery)
            .Include(b => b.Boat)
            .Where(x => x.IsDeleted == false)
            .ToListAsync();

        if (query is null)
        {
            return null;
        }

        return query.Select(MapToDto);
    }

    /// <summary>
    /// Retrieves a booking by its ID.
    /// </summary>
    /// <param name="id">The ID of the booking to retrieve.</param>
    /// <returns>The booking details if found; otherwise, null.</returns>
    /// <exception cref="ArgumentException">Thrown when the booking ID is null or empty.</exception>
    public async Task<BookingDto.ViewBooking> GetBookingById(string id)
    {
        CheckBookingIdNullOrWhiteSpace(id);
        // Changed method so that DTO creation is out of the LINQ Query
        // You need to avoid using methods with optional parameters directly
        // in the LINQ query that EF is trying to translate
        var query = await _dbContext.Bookings
            .Include(x => x.Battery)
            .Include(x => x.Boat)
            .FirstOrDefaultAsync(x => x.Id.Equals(id) && x.IsDeleted == false);

        if (query == null)
        {
            return null;
        }

        return MapToDto(query);
    }

    /// <summary>
    /// Creates a new booking.
    /// </summary>
    /// <param name="booking">The booking details to create.</param>
    /// <returns>The created booking details.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the user ID is null or empty, or when the booking date is invalid.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the user has reached the maximum number of bookings allowed, or when the booking already exists on the specified date.
    /// </exception>
    /// <exception cref="UserNotFoundException">Thrown when the user does not exist.</exception>
    public async Task<BookingDto.ViewBooking> CreateBookingAsync(BookingDto.NewBooking booking)
    {
        await ValidateUser(booking.userId); // Await the call to ensure exceptions are caught

        // Check if user has not reached the maximum allowed bookings
        if (await _validationService.CheckUserMaxBookings(booking.userId))
        {
            throw new InvalidOperationException("You have reached the maximum number of bookings allowed.");
        }

        // Check if the requested booking is still available
        if (await _validationService.BookingExists(booking.bookingDate))
        {
            throw new InvalidOperationException("Booking already exists on this date");
        }

        // Check if requested booking is within the set time range
        if (!CheckWithinDateRange(booking.bookingDate))
        {
            throw new ArgumentException(
                $"Invalid date selection. Please choose a date that is at least {_minReservationDays} days from " +
                $"today and no more than {_maxReservationDays} days ahead.");
        }

        var entity = new Booking(
            timeSlot: TimeSlotEnumExtensions.ToTimeSlot(booking.bookingDate.Hour),
            bookingDate: booking.bookingDate,
            userId: booking.userId
        );
        var created = _dbContext.Bookings.Add(entity);
        int response = await _dbContext.SaveChangesAsync();

        return MapToDto(created.Entity);
    }

    /// <summary>
    /// Updates an existing booking.
    /// </summary>
    /// <param name="booking">The booking details to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    /// <exception cref="ArgumentException">Thrown when the booking ID is null or empty.</exception>
    /// <exception cref="Exception">Thrown when the booking is not found.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the new booking date is outside the allowed range.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when a booking already exists on the new date.</exception>
    public async Task<bool> UpdateBookingAsync(BookingDto.UpdateBooking booking)
    {
        CheckBookingIdNullOrWhiteSpace(booking.bookingId);

        var entity = await _dbContext.Bookings.FindAsync(booking.bookingId) ?? throw new Exception("Booking not found");

        if (booking.bookingDate != null && booking.bookingDate != entity.BookingDate)
        {
            // Check if the new booking date is within the allowed range (+3 to +30 days from today)
            if (booking.bookingDate.Value < DateTime.UtcNow.Date.AddDays(_minReservationDays) ||
                booking.bookingDate.Value > DateTime.UtcNow.Date.AddDays(_maxReservationDays))
            {
                throw new ArgumentOutOfRangeException(nameof(booking.bookingDate),
                    $"Booking date must be between {_minReservationDays} and {_maxReservationDays} days from today.");
            }

            if (await _validationService.BookingExists(booking.bookingDate.Value))
            {
                throw new InvalidOperationException("Booking already exists on this date");
            }

            entity.BookingDate = booking.bookingDate.Value;
        }

        _dbContext.Bookings.Update(entity);
        int response = await _dbContext.SaveChangesAsync();

        return response > 0;
    }

    /// <summary>
    /// Deletes a booking by its ID.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to delete.</param>
    /// <returns>A boolean indicating whether the deletion was successful.</returns>
    /// <exception cref="ArgumentException">Thrown when the booking ID is null or empty.</exception>
    /// <exception cref="Exception">Thrown when the booking is not found.</exception>
    public async Task<bool> DeleteBookingAsync(string bookingId)
    {
        CheckBookingIdNullOrWhiteSpace(bookingId);

        var entity = await _dbContext.Bookings.FindAsync(bookingId) ?? throw new Exception("Booking not found");

        entity.SoftDelete();
        _dbContext.Bookings.Update(entity);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Checks if the booking ID is null or whitespace.
    /// </summary>
    /// <param name="bookingId">The booking ID to check.</param>
    /// <exception cref="ArgumentException">Thrown when the booking ID is null or empty.</exception>
    private static void CheckBookingIdNullOrWhiteSpace(string bookingId)
    {
        if (string.IsNullOrWhiteSpace(bookingId))
        {
            throw new ArgumentException("Booking ID cannot be null or empty.", nameof(bookingId));
        }
    }


    /// <summary>
    /// Retrieves all bookings for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user whose bookings are to be retrieved.</param>
    /// <returns>A collection of the user's bookings, or null if no bookings are found.</returns>
    /// <exception cref="ArgumentException">Thrown when the user ID is null or empty.</exception>
    /// <exception cref="UserNotFoundException">Thrown when the user does not exist.</exception>
    public async Task<IEnumerable<BookingDto.ViewBooking>?> GetAllUserBookings(string userId)
    {
        await ValidateUser(userId); // Await the call to ensure exceptions are caught

        // Changed method so that DTO creation is out of the LINQ Query
        // You need to avoid using methods with optional parameters directly
        // in the LINQ query that EF is trying to translate
        var query = await _dbContext.Bookings
            .Include(b => b.Battery)
            .Include(b => b.Boat)
            .Where(x => x.UserId.Equals(userId))
            .OrderByDescending(x => x.BookingDate)
            .ToListAsync();

        if (query == null)
        {
            return null;
        }

        return query.Select(MapToDto).OrderByDescending(b => b.status == BookingStatus.OPEN) 
            .ThenByDescending(b => b.bookingDate)
            .ToList();;
    }

    /// <summary>
    /// Retrieves all future bookings for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user whose future bookings are to be retrieved.</param>
    /// <returns>A collection of the user's future bookings, or null if no bookings are found.</returns>
    /// <exception cref="ArgumentException">Thrown when the user ID is null or empty.</exception>
    /// <exception cref="UserNotFoundException">Thrown when the user does not exist.</exception>
    public async Task<IEnumerable<BookingDto.ViewBooking>?> GetFutureUserBookings(string userId)
    {
        await ValidateUser(userId); // Await the call to ensure exceptions are caught

        // Changed method so that DTO creation is out of the LINQ Query
        // You need to avoid using methods with optional parameters directly
        // in the LINQ query that EF is trying to translate
        var query = await _dbContext.Bookings
            .Include(x => x.Battery)
            .Include(x => x.Boat)
            .Where(x => x.UserId.Equals(userId) && x.IsDeleted == false && x.BookingDate.Date >= DateTime.Now.Date)
            .OrderByDescending(x => x.BookingDate)
            .ToListAsync();

        if (query == null)
        {
            return null;
        }

        return query.Select(MapToDto);
    }

    /// <summary>
    /// Retrieves all past bookings for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user whose past bookings are to be retrieved.</param>
    /// <returns>A collection of the user's past bookings, or null if no bookings are found.</returns>
    /// <exception cref="ArgumentException">Thrown when the user ID is null or empty.</exception>
    /// <exception cref="UserNotFoundException">Thrown when the user does not exist.</exception>
    public async Task<IEnumerable<BookingDto.ViewBooking>?> GetPastUserBookings(string userId)
    {
        await ValidateUser(userId); // Await the call to ensure exceptions are caught

        var query = await _dbContext.Bookings
            .Include(x => x.Battery)
            .Include(x => x.Boat)
            .Where(x => x.UserId.Equals(userId) && x.IsDeleted == false && x.BookingDate < DateTime.Now)
            .OrderByDescending(x => x.BookingDate)
            .ToListAsync();

        if (query == null)
        {
            return null;
        }

        return query.Select(MapToDto);
    }

    /// <summary>
    /// Maps a Booking entity to a BookingDto.ViewBooking.
    /// </summary>
    /// <param name="booking">The booking entity to map.</param>
    /// <returns>A BookingDto.ViewBooking object containing the mapped details.</returns>
    private BookingDto.ViewBooking MapToDto(Booking booking)
    {
        var battery = new BatteryDto.ViewBattery();
        if (booking.Battery != null)
        {
            battery = new BatteryDto.ViewBattery()
            {
                name = booking.Battery.Name,
                countBookings = booking.Battery.CountBookings,
                listComments = booking.Battery.ListComments,
            };
        }

        var boat = new BoatDto.ViewBoat();
        
        
        //todo status toevoegen aan DB for refunded
        BookingStatus status = BookingStatusHelper.GetBookingStatus(booking.IsDeleted, false, booking.BookingDate,booking.Boat != null && !booking.Boat.Name.IsNullOrEmpty());
        
        if (booking.Boat != null && !booking.Boat.Name.IsNullOrEmpty())
        {
            boat = new BoatDto.ViewBoat()
            {
                name = booking.Boat.Name,
                countBookings = booking.Boat.CountBookings,
                listComments = booking.Boat.ListComments,
            };
        }
         

        var contact = new UserDto.UserDetails
        (
            "auth0|6713ad784fda04f4b9ae2165",
            "John",
            "Doe",
            "john.doe@gmail.com",
            "09/123.45.67",
            new AddressDto.GetAdress
            {
                Street = StreetEnum.DOKNOORD,
                HouseNumber = "35",
                Bus = "3a"
            },
            [new RoleDto() { Name = RolesEnum.User }],
            new DateTime(1990, 1, 1)
        );
        
        
        return new BookingDto.ViewBooking()
        {
            userId = booking.UserId,
            bookingId = booking.Id,
            bookingDate = booking.BookingDate,
            boat = boat,
            battery = battery,
            status = status,
            contact = contact,
            timeSlot = TimeSlotEnumExtensions.ToTimeSlot(booking.BookingDate.Hour),
        };
    }

    /// <summary>
    /// Retrieves all taken timeslots within a specified date range.
    /// </summary>
    /// <param name="startDate">The start date of the range.</param>
    /// <param name="endDate">The end date of the range.</param>
    /// <returns>A collection of taken timeslots within the specified date range.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the start date or end date is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the start date is after the end date.</exception>
    public async Task<IEnumerable<BookingDto.ViewBookingCalender>?> GetTakenTimeslotsInDateRange(DateTime? startDate,
        DateTime? endDate)
    {
        // Check validity of parameters
        if (startDate == null)
        {
            throw new ArgumentNullException(nameof(startDate));
        }

        if (endDate == null)
        {
            throw new ArgumentNullException(nameof(endDate));
        }

        // Validate the date range
        if (startDate.Value.Date > endDate.Value.Date)
        {
            throw new ArgumentException("Start date must be before the end date.", nameof(startDate));
        }

        // Get all bookings from the db that are between start and enddate (limits included)
        List<Booking> bookings = await _dbContext.Bookings
            .Where(booking => booking.BookingDate.Date >= startDate && booking.BookingDate.Date <= endDate &&
                              booking.IsDeleted == false)
            .ToListAsync();

        return bookings.Select(booking => ViewBookingCalenderFromBooking(booking, true)).ToList();
    }

    /// <summary>
    /// Retrieves all free timeslots within a specified date range.
    /// </summary>
    /// <param name="startDate">The start date of the range.</param>
    /// <param name="endDate">The end date of the range.</param>
    /// <returns>A collection of free timeslots within the specified date range.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the start date or end date is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the start date is after the end date.</exception>
    public async Task<IEnumerable<BookingDto.ViewBookingCalender>?> GetFreeTimeslotsInDateRange(DateTime? startDate,
        DateTime? endDate)
    {
        int NON_BOOKABLE_DAYS_AFTER_TODAY = 2;

        // Check validity of parameters
        if (startDate == null)
        {
            throw new ArgumentNullException(nameof(startDate));
        }

        if (endDate == null)
        {
            throw new ArgumentNullException(nameof(endDate));
        }

        // Validate the date range
        if (startDate.Value.Date > endDate.Value.Date)
        {
            throw new ArgumentException("Start date must be before the end date.", nameof(startDate));
        }

        // if there is no possible free timeslot. (cannot book before or on the enddate) return empty list (shortcut)
        if (endDate <= DateTime.Now.AddDays(NON_BOOKABLE_DAYS_AFTER_TODAY))
        {
            return new List<BookingDto.ViewBookingCalender>();
        }

        // Get all bookings from the db that are between startdate and enddate (limits included)
        List<Booking> bookings = await _dbContext.Bookings
            .Where(booking => booking.BookingDate.Date >= startDate && booking.BookingDate.Date <= endDate &&
                              booking.IsDeleted == false)
            .ToListAsync();

        //generate all possible timeslots
        List<BookingDto.ViewBookingCalender> allPossibleTimeslotsInRange = new List<BookingDto.ViewBookingCalender>();
        for (DateTime date = startDate.Value; date <= endDate; date = date.AddDays(1))
        {
            if (date <= DateTime.Now.AddDays(NON_BOOKABLE_DAYS_AFTER_TODAY))
            {
                continue;
            }

            foreach (TimeSlot timeSlot in Enum.GetValues(typeof(TimeSlot)))
            {
                if (timeSlot == TimeSlot.None)
                {
                    continue;
                }

                allPossibleTimeslotsInRange.Add(
                    new BookingDto.ViewBookingCalender
                    {
                        BookingDate = date,
                        TimeSlot = timeSlot,
                        Available = true
                    }
                );
            }
        }

        // return all possible timeslots without the occupied ones
        return allPossibleTimeslotsInRange
            .Where(slot => !bookings.Exists(booking =>
                booking.BookingDate.Date == slot.BookingDate.Date && booking.GetTimeSlot() == slot.TimeSlot))
            .ToList();
    }

    /// <summary>
    /// Maps a Booking entity to a BookingDto.ViewBookingCalender.
    /// </summary>
    /// <param name="booking">The booking entity to map.</param>
    /// <param name="Available">Indicates whether the timeslot is available.</param>
    /// <returns>A BookingDto.ViewBookingCalender object containing the mapped details.</returns>
    private static BookingDto.ViewBookingCalender ViewBookingCalenderFromBooking(Booking booking, bool Available)
    {
        return new BookingDto.ViewBookingCalender
        {
            BookingDate = booking.BookingDate,
            TimeSlot = booking.GetTimeSlot(),
            Available = Available
        };
    }

    /// <summary>
    /// Checks if the given booking date is within the allowed reservation date range.
    /// </summary>
    /// <param name="bookingDate">The booking date to check.</param>
    /// <returns>True if the booking date is within the allowed range; otherwise, false.</returns>
    private bool CheckWithinDateRange(DateTime bookingDate)
    {
        return DateTime.Now.AddDays(_minReservationDays) <= bookingDate &&
               bookingDate <= DateTime.Now.AddDays(_maxReservationDays);
    }


    /// <summary>
    /// Validates the existence of a user by their ID.
    /// </summary>
    /// <param name="userId">The ID of the user to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the user ID is null or empty.</exception>
    /// <exception cref="UserNotFoundException">Thrown when the user does not exist.</exception>
    private async Task ValidateUser(string userId)
    {
        CheckUserIdNullOrWhiteSpace(userId);

        bool userExists = await _validationService.CheckUserExistsAsync(userId);
        if (!userExists)
        {
            throw new UserNotFoundException("User with given id was not found.");
        }
    }

    /// <summary>
    /// Checks if the user ID is null or whitespace.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <exception cref="ArgumentException">Thrown when the user ID is null or empty.</exception>
    private static void CheckUserIdNullOrWhiteSpace(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
        }
    }
}