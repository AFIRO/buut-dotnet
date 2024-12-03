using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Rise.Domain.Bookings;
using Rise.Persistence;
using Rise.Server.Settings;
using Rise.Shared.Bookings;
using Rise.Shared.Enums;
using Rise.Shared.Services;
using Rise.Shared.Users;
using Rise.Shared.Boats;
using Microsoft.Extensions.Logging;

namespace Rise.Services.Bookings;

/// <summary>
/// Provides services for managing bookings.
/// </summary>
public class BookingService : IBookingService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly int _minReservationDays;
    private readonly int _maxReservationDays;
    private readonly IValidationService _validationService;    
    private static readonly int NonBookableDaysAfterToday = 2;
    private readonly ILogger<BookingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BookingService"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="options">The booking settings options.</param>
    /// <param name="validationService">The validation service.</param>
    /// <param name="logger">The logger.</param>
    public BookingService(ApplicationDbContext dbContext, IOptions<BookingSettings> options,
        IValidationService validationService, ILogger<BookingService> logger)
    {
        _minReservationDays = options.Value.MinReservationDays;
        _maxReservationDays = options.Value.MaxReservationDays;
        _validationService = validationService;
        _logger = logger;

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
    public async Task<IEnumerable<BookingDto.ViewBooking>?> GetAllAsync()
    {
        try
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
                _logger.LogWarning("No bookings found");
                return Enumerable.Empty<BookingDto.ViewBooking>();
            }
            _logger.LogInformation("Successfully fetched {Count} bookings.", query.Count);
            return query.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching all bookings.");
            throw;
        }
    }

    /// <summary>
    /// Retrieves a booking by its ID.
    /// </summary>
    /// <param name="id">The ID of the booking to retrieve.</param>
    /// <returns>The booking details if found; otherwise, null.</returns>
    /// <exception cref="ArgumentException">Thrown when the booking ID is null or empty.</exception>
    public async Task<BookingDto.ViewBooking?> GetBookingById(string id)
    {
        try
        {
            CheckBookingIdNullOrWhiteSpace(id);
            // Changed method so that DTO creation is out of the LINQ Query
            // You need to avoid using methods with optional parameters directly
            // in the LINQ query that EF is trying to translate
            var query = await _dbContext.Bookings
                .Include(x => x.Battery)
                .Include(x => x.Boat)
                .FirstOrDefaultAsync(x => x.Id.Equals(id) && x.IsDeleted == false);

            if (query is null)
            {
                _logger.LogWarning("Booking with ID {Id} not found.", id);
                return null;
            }

            _logger.LogInformation("Successfully fetched booking with ID {Id}.", id);
            return MapToDto(query);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid booking ID provided.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching booking with ID {Id}.", id);
            throw new Exception("Error occurred while fetching booking.", ex);
        }
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
        try
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
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid booking details provided.");
            throw;
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User with ID {Id} was not found.", booking.userId);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Booking already exists on this date.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating a new booking.");
            throw new Exception("Error occurred while creating a new booking.", ex);
        }
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
        try
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

            _logger.LogInformation("Booking with ID {BookingId} updated successfully.", booking.bookingId);
            return response > 0;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid booking details provided.");
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Booking already exists on this date.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating booking with ID {Id}.", booking.bookingId);
            throw new Exception("Error occurred while updating booking.", ex);
        }
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
        try
        {
            CheckBookingIdNullOrWhiteSpace(bookingId);

            var entity = await _dbContext.Bookings.FindAsync(bookingId) ?? throw new Exception("Booking not found");

            entity.SoftDelete();
            _dbContext.Bookings.Update(entity);
            int response = await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Booking with ID {BookingId} deleted successfully.", bookingId);
            return response > 0;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid booking ID provided for booking deletion.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting booking with ID {Id}.", bookingId);
            throw new Exception("Error occurred while deleting booking.", ex);
        }
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
        try
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

            if (query is null)
            {
                _logger.LogInformation("No bookings found for user {UserId}.", userId);
                return Enumerable.Empty<BookingDto.ViewBooking>();
            }

            _logger.LogInformation("{Count} bookings retrieved for user {UserId}.", query.Count, userId);
            return query.Select(MapToDto).OrderByDescending(b => b.status == BookingStatus.OPEN)
                .ThenByDescending(b => b.bookingDate)
                .ToList();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid input provided for fetching user bookings.");
            throw;
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User with ID {Id} was not found.", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching all bookings for user with ID {Id}.", userId);
            throw new Exception("Error occurred while fetching all bookings for user.", ex);
        }
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
        try
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

            if (query is null)
            {
                _logger.LogInformation("No future bookings found for user {UserId}.", userId);
                return Enumerable.Empty<BookingDto.ViewBooking>();
            }

            _logger.LogInformation("{Count} future bookings retrieved for user {UserId}.", query.Count, userId);
            return query.Select(MapToDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid input provided for fetching future user bookings.");
            throw;
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User with ID {Id} was not found.", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching future bookings for user with ID {Id}.", userId);
            throw new Exception("Error occurred while fetching future bookings for user.", ex);
        }
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
        try
        {
            await ValidateUser(userId); // Await the call to ensure exceptions are caught

            var query = await _dbContext.Bookings
                .Include(x => x.Battery)
                .Include(x => x.Boat)
                .Where(x => x.UserId.Equals(userId) && x.IsDeleted == false && x.BookingDate < DateTime.Now)
                .OrderByDescending(x => x.BookingDate)
                .ToListAsync();

            if (query is null)
            {
                _logger.LogInformation("No past bookings found for user {UserId}.", userId);
                return Enumerable.Empty<BookingDto.ViewBooking>();
            }

            _logger.LogInformation("{Count} past bookings retrieved for user {UserId}.", query.Count, userId);
            return query.Select(MapToDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid input provided for fetching past user bookings.");
            throw;
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User with ID {Id} was not found.", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching past bookings for user with ID {Id}.", userId);
            throw new Exception("Error occurred while fetching past bookings for user.", ex);
        }
    }

    /// <summary>
    /// Maps a Booking entity to a BookingDto.ViewBooking.
    /// </summary>
    /// <param name="booking">The booking entity to map.</param>
    /// <returns>A BookingDto.ViewBooking object containing the mapped details.</returns>
    private BookingDto.ViewBooking MapToDto(Booking booking)
    {
        var battery = new BatteryDto.ViewBattery();
        if (booking.Battery is not null)
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
        BookingStatus status = BookingStatusHelper.GetBookingStatus(booking.IsDeleted, false, booking.BookingDate, booking.Boat != null && !booking.Boat.Name.IsNullOrEmpty());

        if (booking.Boat is not null && !booking.Boat.Name.IsNullOrEmpty())
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
        try
        {
            // Check validity of parameters
            if (startDate == null || endDate == null)
            {
                _logger.LogWarning("Invalid date range provided for fetching taken timeslots.");
                throw new ArgumentNullException(startDate == null ? nameof(startDate) : nameof(endDate));
            }


            // Validate the date range
            if (startDate.Value.Date > endDate.Value.Date)
            {
                _logger.LogWarning("Start date {StartDate} is after end date {EndDate}.", startDate, endDate);
                throw new ArgumentException("Start date must be before the end date.", nameof(startDate));
            }

            _logger.LogInformation("Fetching taken timeslots between {StartDate} and {EndDate}.", startDate, endDate);

            // Get all bookings from the db that are between start and enddate (limits included)
            var bookings = await _dbContext.Bookings
                  .Where(booking => booking.BookingDate.Date >= startDate && booking.BookingDate.Date <= endDate &&
                                    booking.IsDeleted == false)
                  .ToListAsync();

            if (bookings is null)
            {
                _logger.LogInformation("No taken timeslots found between {StartDate} and {EndDate}.", startDate, endDate);
                return Enumerable.Empty<BookingDto.ViewBookingCalender>();
            }

            _logger.LogInformation("{Count} taken timeslots retrieved between {StartDate} and {EndDate}.", bookings.Count, startDate, endDate);
            return bookings.Select(booking => ViewBookingCalenderFromBooking(booking, true)).ToList();
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Invalid input provided for fetching taken timeslots.");
            throw;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid date range provided.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while fetching taken timeslots.");
            throw new Exception("An unexpected error occurred while retrieving taken timeslots.", ex);
        }
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
        try
        {
            const int NON_BOOKABLE_DAYS_AFTER_TODAY = 2;

            // Check validity of parameters
            if (startDate == null || endDate == null)
            {
                _logger.LogWarning("Invalid date range provided for fetching free timeslots.");
                throw new ArgumentNullException(startDate == null ? nameof(startDate) : nameof(endDate));
            }

            // Validate the date range
            if (startDate.Value.Date > endDate.Value.Date)
            {
                _logger.LogWarning("Start date {StartDate} is after end date {EndDate}.", startDate, endDate);
                throw new ArgumentException("Start date must be before the end date.", nameof(startDate));
            }

            // if there is no possible free timeslot. (cannot book before or on the enddate) return empty list (shortcut)
            if (endDate <= DateTime.Now.AddDays(NON_BOOKABLE_DAYS_AFTER_TODAY))
            {
                _logger.LogInformation("No free timeslots available before {EndDate}.", endDate);
                return Enumerable.Empty<BookingDto.ViewBookingCalender>();
            }

            // Get all bookings from the db that are between startdate and enddate (limits included)
            var bookings = await _dbContext.Bookings
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
            var freeTimeslots = allPossibleTimeslotsInRange
                .Where(slot => !bookings.Exists(booking =>
                    booking.BookingDate.Date == slot.BookingDate.Date && booking.GetTimeSlot() == slot.TimeSlot))
                .ToList();

            _logger.LogInformation("{Count} free timeslots retrieved between {StartDate} and {EndDate}.", freeTimeslots.Count, startDate, endDate);
            return freeTimeslots;
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Invalid input provided for fetching free timeslots.");
            throw;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid date range provided.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while fetching free timeslots.");
            throw new Exception("An unexpected error occurred while retrieving free timeslots.", ex);
        }
    }

    public async Task<int> GetAmountOfFreeTimeslotsForWeek()
    {
        DateTime today = DateTime.Today;
        DateTime endOfWeek = GetSundayForCurrentOrNextWeek();
        int freeTimeSlots = 0;
        
        var targetDate = today.AddDays(NonBookableDaysAfterToday);

        // Check if the target date is in the next week
        if (targetDate.DayOfWeek < today.DayOfWeek)
        {
            return 0;
        }

        // Iterate over each day in the current week
        for (DateTime date = today; date <= endOfWeek; date = date.AddDays(1))
        {
            // Iterate over each timeslot
            foreach (TimeSlot timeSlot in Enum.GetValues(typeof(TimeSlot)))
            {
                if (timeSlot == TimeSlot.None)
                    continue;

                // Check if the timeslot is booked
                if (!IsBooked(date, timeSlot))
                {
                    freeTimeSlots++;
                }
            }
        }

        return freeTimeSlots;
    }
    
    private DateTime GetSundayForCurrentOrNextWeek()
    {
        DateTime today = DateTime.Today;

        // Calculate how many days to add to get to the next Sunday
        int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)today.DayOfWeek + 7) % 7;

        if (daysUntilSunday == 0)
        {
            daysUntilSunday = 7; // if today is Sunday, take next Sunday
        }

        return today.AddDays(daysUntilSunday);
    }

    public async Task<BookingDto.ViewBookingCalender> GetFirstFreeTimeSlot()
    {
        DateTime bookingDate = DateTime.Today.AddDays(NonBookableDaysAfterToday);

        while (true) // Keep iterating through dates until a free slot is found
        {
            foreach (TimeSlot timeSlot in Enum.GetValues(typeof(TimeSlot)))
            {
                if (timeSlot == TimeSlot.None)
                    continue;

                // Check if the current date and time slot is booked
                if (!IsBooked(bookingDate, timeSlot))
                {
                    // Return the first free booking
                    return new BookingDto.ViewBookingCalender
                    {
                        BookingDate = bookingDate,
                        TimeSlot = timeSlot,
                        Available = true
                    };
                }
            }

            // Move to the next day
            bookingDate = bookingDate.AddDays(1);
        }
    }

    private bool IsBooked(DateTime date, TimeSlot timeSlot)
    {
        // Adjust the date to match the specific time slot's hour
        var startHour = timeSlot.GetStartHour();
        var bookingStartTime = date.Date.AddHours(startHour);

        // Query the database to check if a booking exists for this date and time slot
        return _dbContext.Bookings.Any(b => b.BookingDate == bookingStartTime && !b.IsDeleted);
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
            _logger.LogWarning("User with ID {Id} was not found.", userId);
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