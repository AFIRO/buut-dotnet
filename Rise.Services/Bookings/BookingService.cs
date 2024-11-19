using System.Text.Json;
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

        if (query == null)
        {
            return null;
        }

        return query.Select(MapToDto);
    }

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

    public async Task<BookingDto.ViewBooking> CreateBookingAsync(BookingDto.NewBooking booking)
    {
        await ValidateUser(booking.userId); // Await the call to ensure exceptions are caught

        //Check if user has not reached the maximum allowed bookings
        if (await _validationService.CheckUserMaxBookings(booking.userId))
        {
            throw new InvalidOperationException("You have reached the maximum number of bookings allowed.");
        }

        //Check if the requested booking is still available
        if (await _validationService.BookingExists(booking.bookingDate))
        {
            throw new InvalidOperationException("Booking already exists on this date");
        }

        //Check if requested booking is within the set timerange
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

//         /*entity.Boat = booking.boat ?? entity.Boat;
//         entity.Battery = booking.battery ?? entity.Battery;
//
        _dbContext.Bookings.Update(entity);
        int response = await _dbContext.SaveChangesAsync();

        return response > 0;
    }

    public async Task<bool> DeleteBookingAsync(string bookingId)
    {
        CheckBookingIdNullOrWhiteSpace(bookingId);

        var entity = await _dbContext.Bookings.FindAsync(bookingId) ?? throw new Exception("Booking not found");

        entity.SoftDelete();
        _dbContext.Bookings.Update(entity);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    private static void CheckBookingIdNullOrWhiteSpace(string bookingId)
    {
        if (string.IsNullOrWhiteSpace(bookingId))
        {
            throw new ArgumentException("Booking ID cannot be null or empty.", nameof(bookingId));
        }
    }


    public async Task<IEnumerable<BookingDto.ViewBooking>?> GetAllUserBookings(string userId)
    {
        await ValidateUser(userId); // Await the call to ensure exceptions are caught

        // Changed method so that DTO creation is out of the LINQ Query
        // You need to avoid using methods with optional parameters directly
        // in the LINQ query that EF is trying to translate
        var query = await _dbContext.Bookings
            .Include(b => b.Battery)
            .Include(b => b.Boat)
            .Where(x => x.UserId.Equals(userId) && x.IsDeleted == false)
            .OrderByDescending(x => x.BookingDate)
            .ToListAsync();

        if (query == null)
        {
            return null;
        }

        return query.Select(MapToDto);
    }

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
        if (booking.Boat != null)
        {
            boat = new BoatDto.ViewBoat()
            {
                name = booking.Boat.Name,
                countBookings = booking.Boat.CountBookings,
                listComments = booking.Boat.ListComments,
            };
        }

        return new BookingDto.ViewBooking()
        {
            bookingId = booking.Id,
            bookingDate = booking.BookingDate,
            battery = battery,
            boat = boat,
            timeSlot = TimeSlotEnumExtensions.ToTimeSlot(booking.BookingDate.Hour),
        };
    }

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

    private static BookingDto.ViewBookingCalender ViewBookingCalenderFromBooking(Booking booking, bool Available)
    {
        return new BookingDto.ViewBookingCalender
        {
            BookingDate = booking.BookingDate,
            TimeSlot = booking.GetTimeSlot(),
            Available = Available
        };
    }

    private bool CheckWithinDateRange(DateTime bookingDate)
    {
        return DateTime.Now.AddDays(_minReservationDays) <= bookingDate &&
               bookingDate <= DateTime.Now.AddDays(_maxReservationDays);
    }

    private async Task ValidateUser(string userId)
    {
        CheckUserIdNullOrWhiteSpace(userId);
        
        bool userExists = await _validationService.CheckUserExistsAsync(userId);
        if (!userExists)
        {
            throw new UserNotFoundException("User with given id was not found.");
        }
    }
    
    private static void CheckUserIdNullOrWhiteSpace(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
        }
    }
}