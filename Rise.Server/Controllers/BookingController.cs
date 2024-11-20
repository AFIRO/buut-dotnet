using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Rise.Domain.Bookings;
using Rise.Server.Settings;
using Rise.Services.Bookings;
using Rise.Shared.Bookings;

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
    private readonly int _minReservationDays;
    private readonly int _maxReservationDays;

    /// <summary>
    /// Initializes a new instance of the <see cref="BookingController"/> class with the specified booking service.
    /// </summary>
    /// <param name="bookingService">The booking service that handles booking operations.</param>
    public BookingController(IBookingService bookingService, IOptions<BookingSettings> options)
    {
        _bookingService = bookingService;
        _minReservationDays = options.Value.MinReservationDays;
        _maxReservationDays = options.Value.MaxReservationDays;
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
            return Ok(bookings);
        }
        catch (Exception ex)
        {
            // _logger.LogError(ex, "An error occurred while retrieving all bookings.");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
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
            return BadRequest("Booking ID cannot be null or empty.");
        }
        try
        {
            var booking = await _bookingService.GetBookingById(id);

            if (booking == null)
            {
                return NotFound($"Booking with ID '{id}' was not found.");
            }

            return Ok(booking);
        }
        catch (Exception ex)
        {
            // _logger.LogError(ex, "An error occurred while retrieving the booking with ID '{BookingId}'.", id);

            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
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
        if (booking == null)
        {
            return BadRequest("Booking details cannot be null.");
        }

        try
        {
            var createdBooking  = await _bookingService.CreateBookingAsync(booking);
            return CreatedAtAction(nameof(Get), new { id = createdBooking.bookingId }, createdBooking);
        }
        catch (Exception ex)
        {
            // _logger.LogError(ex, "An error occurred while creating a new booking.");
            return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while processing your request: {ex.Message}");
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
        if (string.IsNullOrWhiteSpace(id) || booking == null || booking.bookingId != id)
        {
            return BadRequest("Invalid booking ID or details.");
        }
        try
        {
            var updated = await _bookingService.UpdateBookingAsync(booking);
            if (updated)
            {
                return NoContent();
            }

            return NotFound($"Booking with ID '{id}' was not found.");
        }
        catch (Exception ex)
        {
            // _logger.LogError(ex, "An error occurred while updating the booking with ID '{BookingId}'.", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
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
            return BadRequest("Booking ID cannot be null or empty.");
        }
        try
        {
            var deleted = await _bookingService.DeleteBookingAsync(id);
            if (deleted)
            {
                return NoContent();
            }

            return NotFound($"Booking with ID '{id}' was not found.");
        }
        catch (Exception ex)
        {
            // _logger.LogError(ex, "An error occurred while deleting the booking with ID '{BookingId}'.", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
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
    public async Task<IActionResult> GetBookingsByDateRange([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {   
        if (!startDate.HasValue || !endDate.HasValue)
        {
            return BadRequest("Start date and end date are required.");
        }

        try
        {
            var bookings = await _bookingService.GetTakenTimeslotsInDateRange(startDate, endDate);
            return Ok(bookings);
        }
        catch (ArgumentException ex)
        {
            // _logger.LogWarning(ex, "Invalid date range provided.");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            // _logger.LogError(ex, "An error occurred while retrieving bookings by date range.");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
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
        
        return await GetFreeTimeslotsByDateRange(fixedStartDate, fixedEndDate);
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
    public async Task<IActionResult> GetFreeTimeslotsByDateRange([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {   
        if (!startDate.HasValue || !endDate.HasValue)
        {
            return BadRequest("Start date and end date are required.");
        }

        try
        {
            var freeTimeslots = await _bookingService.GetFreeTimeslotsInDateRange(startDate.Value, endDate.Value);
            return Ok(freeTimeslots);
        }
        catch (ArgumentException ex)
        {
            // _logger.LogWarning(ex, "Invalid date range provided.");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            // _logger.LogError(ex, "An error occurred while retrieving free timeslots by date range.");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
        }
    }
    
}