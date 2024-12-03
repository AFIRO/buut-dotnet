using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Rise.Domain.Bookings;
using Rise.Server.Settings;
using Rise.Services.Events;
using Rise.Services.Events.Booking;
using Rise.Shared.Bookings;
using Rise.Shared.Enums;

namespace Rise.Server.Controllers;

/// <summary>
/// API controller for managing booking-related operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "User")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IEventDispatcher _eventDispatcher;
    private readonly int _minReservationDays;
    private readonly int _maxReservationDays;
    private readonly ILogger<BookingController> _logger;



    /// <summary>
    /// Initializes a new instance of the <see cref="BookingController"/> class with the specified booking service.
    /// </summary>
    /// <param name="bookingService">The booking service that handles booking operations.</param>
    /// <param name="options">The booking settings options.</param>
    /// <param name="eventDispatcher">The event dispatcher that handles event dispatching.</param>
    public BookingController(IBookingService bookingService, IOptions<BookingSettings> options, IEventDispatcher eventDispatcher, ILogger<BookingController> logger)
    {
        _bookingService = bookingService;
        _minReservationDays = options.Value.MinReservationDays;
        _maxReservationDays = options.Value.MaxReservationDays;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all bookings asynchronously.
    /// </summary>
    /// <returns>List of <see cref="BookingDto"/> objects or <c>null</c> if no bookings are found.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookingDto.ViewBooking>>> GetAllBookings()
    {
        try
        {
            var bookings = await _bookingService.GetAllAsync();
            _logger.LogInformation("Successfully retrieved all bookings.");
            return Ok(bookings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving all bookings.");
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }
    
    /// <summary>
    /// Retrieves the first free timeslot.
    /// </summary>
    /// <returns><see cref="BookingDto"/> viewBookingCalender object or <c>null</c> if no free slot is found.</returns>
    [HttpGet("free/first-timeslot")]
    [AllowAnonymous]
    public async Task<ActionResult<BookingDto.ViewBookingCalender>> GetFirstFreeTimeSlot()
    {
        try
        {
            var timeSlot = await _bookingService.GetFirstFreeTimeSlot();
            return Ok(timeSlot);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while processing your request.");
        }
    }

    /// <summary>
    /// Retrieves a booking by their ID asynchronously.
    /// </summary>
    /// <param name="id">The ID of the booking to retrieve.</param>
    /// <returns>The <see cref="BookingDto"/> object or <c>null</c> if no booking with the specified ID is found.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<BookingDto.ViewBooking>> Get(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            _logger.LogWarning("Booking ID cannot be null or empty.");
            return BadRequest("Booking ID cannot be null or empty.");
        }

        try
        {
            var booking = await _bookingService.GetBookingById(id);

            if (booking is null)
            {
                _logger.LogWarning("Booking with ID {BookingId} not found.", id);
                return NotFound($"Booking with ID '{id}' was not found.");
            }

            _logger.LogInformation("Successfully retrieved booking with ID {BookingId}.", id);
            return Ok(booking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the booking with ID '{BookingId}'.", id);
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }

    /// <summary>
    /// Creates a new booking asynchronously.
    /// </summary>
    /// <param name="booking">The <see cref="BookingDto.NewBooking"/> object containing booking details to create.</param>
    /// <returns>The created <see cref="BookingDto.NewBooking"/> object or <c>null</c> if the booking creation fails.</returns>
    [HttpPost]
    public async Task<ActionResult> Post([FromBody] BookingDto.NewBooking? booking)
    {
        if (booking is null)
        {
            _logger.LogWarning("Booking details cannot be null.");
            return BadRequest("Booking details cannot be null.");
        }

        try
        {
            var createdBooking = await _bookingService.CreateBookingAsync(booking);
            if (createdBooking is null)
            {
                _logger.LogWarning("Booking creation failed for user {UserId}.", booking.userId);
                return BadRequest("Booking creation failed.");
            }

            // Dispatch the event
            var bookingCreatedEvent = new BookingCreatedEvent(createdBooking.bookingId, createdBooking.userId,
                createdBooking.bookingDate, createdBooking.timeSlot);
            await _eventDispatcher.DispatchAsync(bookingCreatedEvent);

            _logger.LogInformation("Booking successfully created with ID {BookingId}.", createdBooking.bookingId);
            return CreatedAtAction(nameof(Get), new { id = createdBooking.bookingId }, createdBooking);
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User with ID {UserId} not found while creating booking.", booking.userId);
            return NotFound(new { message = $"User with ID {booking.userId} was not found." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error occurred while creating booking.");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating a new booking.");
            return StatusCode(500, "An error occurred while processing your request.");
        }

    }

    /// <summary>
    /// Updates an existing booking asynchronously.
    /// </summary>
    /// <param name="id">The id of an existing <see cref="Booking"/></param>
    /// <param name=" booking">The <see cref="BookingDto.UpdateBooking"/> object containing updated booking details.</param>
    /// <returns><c>true</c> if the update is successful; otherwise, <c>false</c>.</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult> Put(string id, [FromBody] BookingDto.UpdateBooking? booking)
    {
        if (string.IsNullOrWhiteSpace(id) || booking is null || booking.bookingId != id)
        {
            _logger.LogWarning("Invalid booking ID or details provided for update.");
            return BadRequest("Invalid booking ID or details.");
        }

        try
        {
            var existingBooking = await _bookingService.GetBookingById(id);
            if (existingBooking is null)
            {
                _logger.LogWarning("Booking with ID {BookingId} not found for update.", id);
                return NotFound($"Booking with ID '{id}' was not found.");
            }

            var updated = await _bookingService.UpdateBookingAsync(booking);
            if (updated)
            {
                // Dispatch the event
                TimeSlot updatedTimeSlot = booking.bookingDate.HasValue
                    ? TimeSlotEnumExtensions.ToTimeSlot(booking.bookingDate.Value.Hour)
                    : existingBooking.timeSlot;

                var bookingUpdatedEvent = new BookingUpdatedEvent(id, existingBooking.userId,
                    existingBooking.bookingDate, existingBooking.timeSlot, booking.bookingDate ?? existingBooking.bookingDate, updatedTimeSlot);
                await _eventDispatcher.DispatchAsync(bookingUpdatedEvent);

                _logger.LogInformation("Booking with ID {BookingId} updated successfully.", id);
                return NoContent(); // Explicitly return NoContentResult
            }

            _logger.LogWarning("Booking with ID {BookingId} update failed.", id);
            return NotFound($"Booking with ID '{id}' was not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating booking with ID {BookingId}.", id);
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }


    /// <summary>
    /// Deletes a booking by their ID asynchronously.
    /// </summary>
    /// <param name="id">The ID of the booking to delete.</param>
    /// <returns><c>true</c> if the deletion is successful; otherwise, <c>false</c>.</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            _logger.LogWarning("Invalid booking ID provided for deletion.");
            return BadRequest("Booking ID cannot be null or empty.");
        }

        try
        {
            var existingBooking = await _bookingService.GetBookingById(id);
            if (existingBooking is null)
            {
                _logger.LogWarning("Booking with ID {BookingId} not found for deletion.", id);
                return NotFound($"Booking with ID '{id}' was not found.");
            }

            var deleted = await _bookingService.DeleteBookingAsync(id);
            if (deleted)
            {
                // Dispatch the event
                var bookingDeletedEvent = new BookingDeletedEvent(id, existingBooking.userId,
                    existingBooking.bookingDate, existingBooking.timeSlot);
                await _eventDispatcher.DispatchAsync(bookingDeletedEvent);

                _logger.LogInformation("Booking with ID {BookingId} deleted successfully.", id);
                return NoContent(); // Explicitly return NoContentResult
            }

            _logger.LogWarning("Booking with ID {BookingId} deletion failed.", id);
            return NotFound($"Booking with ID '{id}' was not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting booking with ID {BookingId}.", id);
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }


    /// <summary>
    /// Retrieves all bookings within a specified date range.
    /// </summary>
    /// <param name="startDate" example="2024-10-01">The start date of the range (inclusive).</param>
    /// <param name="endDate">The end date of the range (inclusive).</param>
    /// <returns>An <see cref="IActionResult"/> containing the list of bookings or an error message if the input is invalid.</returns>
    /// <response code="200">Returns the list of bookings within the date range.</response>
    /// <response code="400">If the date range is invalid or any other argument exception occurs.</response>
    [HttpGet("byDateRange")]
    public async Task<IActionResult> GetBookingsByDateRange([FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        if (!startDate.HasValue || !endDate.HasValue)
        {
            _logger.LogWarning("Start date and end date are required for fetching bookings by date range.");
            return BadRequest("Start date and end date are required.");
        }

        try
        {
            var bookings = await _bookingService.GetTakenTimeslotsInDateRange(startDate, endDate);
            _logger.LogInformation("Successfully retrieved bookings between {StartDate} and {EndDate}.", startDate, endDate);
            return Ok(bookings);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid date range provided.");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving bookings by date range.");
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }

    /// <summary>
    /// Retrieves all free timeslots within a specified date range. Replaces GetBookingsByDateRange -> the server decides which extra timeslots are not available
    /// </summary>
    /// <returns>An <see cref="IActionResult"/> containing the list of bookings or an error message if the input is invalid.</returns>
    /// <response code="200">Returns the list of free timeslots within the date range.</response>
    /// <response code="400">If the date range is invalid or any other argument exception occurs.</response>
    [HttpGet("free")]
    public async Task<IActionResult> GetFreeTimeslotsByDateRange()
    {
        DateTime fixedStartDate = DateTime.UtcNow.Date.AddDays(_minReservationDays);
        DateTime fixedEndDate = DateTime.UtcNow.Date.AddDays(_maxReservationDays);

        try
        {
            var freeTimeSlots = await GetFreeTimeslotsByDateRange(fixedStartDate, fixedEndDate);
            _logger.LogInformation("Successfully retrieved free timeslots between {StartDate} and {EndDate}.", fixedStartDate, fixedEndDate);
            return Ok(freeTimeSlots);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving free timeslots for fixed date range.");
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }

    /// <summary>
    /// Retrieves all free timeslots within a specified date range. Replaces GetBookingsByDateRange -> the server decides which extra timeslots are not available
    /// </summary>
    /// <param name="startDate" example="2024-10-01">The start date of the range (inclusive).</param>
    /// <param name="endDate">The end date of the range (inclusive).</param>
    /// <returns>An <see cref="IActionResult"/> containing the list of bookings or an error message if the input is invalid.</returns>
    /// <response code="200">Returns the list of free timeslots within the date range.</response>
    /// <response code="400">If the date range is invalid or any other argument exception occurs.</response>
    [HttpGet("free/byDateRange")]
    public async Task<IActionResult> GetFreeTimeslotsByDateRange([FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        if (!startDate.HasValue || !endDate.HasValue)
        {
            _logger.LogWarning("Start date and end date are required for fetching free timeslots.");
            return BadRequest("Start date and end date are required.");
        }

        try
        {
            var freeTimeslots = await _bookingService.GetFreeTimeslotsInDateRange(startDate.Value, endDate.Value);
            _logger.LogInformation("Successfully retrieved free timeslots between {StartDate} and {EndDate}.", startDate, endDate);
            return Ok(freeTimeslots);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid date range provided for free timeslots.");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving free timeslots by date range.");
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }
    
    /// <summary>
    /// Retrieves the amount free timeslots for the week
    /// </summary>
    /// <returns>An <see cref="IActionResult"/> containing the amount of free timeslots or an error message if the input is invalid.</returns>
    /// <response code="200">Returns the amount of free timeslots.</response>
    /// <response code="400">If the date range is invalid or any other argument exception occurs.</response>
    [HttpGet("free/count")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAmountOfFreeTimeslotsForWeek()
    { 
        try
        {
            var freeTimeslots = await _bookingService.GetAmountOfFreeTimeslotsForWeek();
            Console.WriteLine("free: " + freeTimeslots);
            return Ok(freeTimeslots);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while processing your request.");
        }
    }

    /// <summary>
    /// Retrieves all bookings asynchronously for specific user.
    /// </summary>
    /// <param name="userid">The ID of the user to retrieve their bookings.</param>
    /// <returns>List of <see cref="BookingDto"/> objects or <c>null</c> if no bookings are found.</returns>
    [HttpGet("user/{userid}")]
    public async Task<IActionResult> GetAllUserBookings(string userid)
    {
        if (string.IsNullOrWhiteSpace(userid))
        {
            _logger.LogWarning("User ID cannot be null or empty for fetching all bookings.");
            return BadRequest("User ID cannot be null or empty.");
        }

        try
        {
            var bookings = await _bookingService.GetAllUserBookings(userid);
            _logger.LogInformation("Successfully retrieved all bookings for user with ID {UserId}.", userid);
            return Ok(bookings);
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User with ID {UserId} not found.", userid);
            return NotFound(new { message = $"User with ID {userid} was not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching all bookings for user with ID {UserId}.", userid);
            return StatusCode(500,
                new { message = "An unexpected error occurred while fetching all bookings.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves future bookings asynchronously for specific user.
    /// </summary>
    /// <param name="userid">The ID of the user to retrieve their future bookings.</param>
    /// <returns><see cref="BookingDto"/> object or <c>null</c> if no booking is found.</returns>
    [HttpGet("user/{userid}/future")]
    public async Task<IActionResult> GetFutureUserBookings(string userid)
    {
        if (string.IsNullOrWhiteSpace(userid))
        {
            _logger.LogWarning("User ID cannot be null or empty for fetching future bookings.");
            return BadRequest("User ID cannot be null or empty.");
        }
        try
        {
            var bookings = await _bookingService.GetFutureUserBookings(userid);
            _logger.LogInformation("Successfully retrieved future bookings for user with ID {UserId}.", userid);
            return Ok(bookings);
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User with ID {UserId} not found.", userid);
            return NotFound(new { message = $"User with ID {userid} was not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching future bookings for user with ID {UserId}.", userid);
            // Handle any other unexpected errors
            return StatusCode(500, new
            {
                message = "An unexpected error occurred while fetching the future bookings.",
                detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Retrieves past bookings asynchronously for specific user.
    /// </summary>
    /// <returns><see cref="BookingDto"/> object or <c>null</c> if no booking is found.</returns>
    [HttpGet("user/{userid}/past")]
    public async Task<IActionResult> GetPastUserBookings(string userid)
    {
        if (string.IsNullOrWhiteSpace(userid))
        {
            _logger.LogWarning("User ID cannot be null or empty for fetching past bookings.");
            return BadRequest("User ID cannot be null or empty.");
        }
        try
        {
            var bookings = await _bookingService.GetPastUserBookings(userid);
            _logger.LogInformation("Successfully retrieved past bookings for user with ID {UserId}.", userid);
            return Ok(bookings);
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User with ID {UserId} not found.", userid);
            return NotFound(new { message = $"User with ID {userid} was not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching past bookings for user with ID {UserId}.", userid);
            // Handle any other unexpected errors
            return StatusCode(500,
                new
                {
                    message = "An unexpected error occurred while fetching the past bookings.",
                    detail = ex.Message
                });
        }
    }
}