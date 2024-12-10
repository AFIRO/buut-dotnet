using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rise.Server.Controllers;
using Rise.Shared.Batteries;
using Rise.Shared.Bookings;
using Rise.Shared.Users;
using System.Security.Claims;

namespace Rise.Server.Controllers
{
    /// <summary>
    /// API controller for managing battery-related operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin, BUUTAgent")]
    public class BatteryController : ControllerBase
    {
        private readonly IBatteryService _batteryService;
        private readonly ILogger<BatteryController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatteryController"/> class.
        /// </summary>
        /// <param name="batteryService">The service to manage battery-related operations.</param>
        public BatteryController(IBatteryService batteryService, ILogger<BatteryController> logger)
        {
            _batteryService = batteryService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all batteries.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains an 
        /// <see cref="ActionResult"/> of type <see cref="IEnumerable{BatteryDto.ViewBattery}"/>, which is the list of all batteries.
        /// </returns>
        /// <response code="200">Returns the list of batteries.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<BatteryDto.ViewBattery>>> GetAllBatteries()
        {
            try
            {
                var batteries = await _batteryService.GetAllAsync();
                return Ok(batteries);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error occurred during retrieval of all batteries");
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        /// <summary>
        /// Creates a new battery.
        /// </summary>
        /// <param name="battery">The details of the new battery to create.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains an 
        /// <see cref="ActionResult"/> that indicates the result of the creation operation.
        /// </returns>
        /// <response code="201">Returns the created battery.</response>
        /// <response code="400">If the input battery details are null.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Post([FromBody] BatteryDto.NewBattery? battery)
        {
            if (battery == null)
            {
                return BadRequest("Battery details can't be null");
            }
            try
            {
                var createdBattery = await _batteryService.CreateAsync(battery);
                return CreatedAtAction(null, null, createdBattery);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error occurred during creation of a new battery");
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        /// <summary>
        /// Retrieves information about the authenticated godparents battery.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains an 
        /// <see cref="ActionResult"/> of type <see cref="BatteryDto.ViewBattery"/>, which is the list of all batteries.
        /// </returns>
        /// <response code="200">Returns the list of batteries.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("godparent/info")]
        [Authorize(Roles = "BUUTAgent")]
        public async Task<ActionResult<BatteryDto.ViewBatteryBuutAgent>> GetGodchildBattery()
        {
            try
            {
                // Get the authenticated user's ID if non existent trow error
                var claim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (claim == null || claim.Value == null)
                {
                    throw new InvalidOperationException("User is not authenticated or NameIdentifier claim is missing.");
                }
                string authenticatedUserId = claim.Value;

                // get the childbattery of the user
                var battery = await _batteryService.GetBatteryByGodparentUserIdAsync(authenticatedUserId);
                return Ok(battery);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
        /// <summary>
        /// Retrieves information about the authenticated godparents battery.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains an 
        /// <see cref="ActionResult"/> of type <see cref="BatteryDto.ViewBattery"/>, which is the list of all batteries.
        /// </returns>
        /// <response code="200">Returns the list of batteries.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("godparent/holder")]
        [Authorize(Roles = "BUUTAgent")]
        public async Task<ActionResult<UserDto.UserContactDetails>> GetGodchildBatteryHolder()
        {
            try
            {
                // Get the authenticated user's ID if non existent trow error
                var claim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (claim == null)
                {
                    throw new InvalidOperationException("User is not authenticated or NameIdentifier claim is missing.");
                }
                string authenticatedUserId = claim.Value;

                // get the childbattery of the user
                var holder = await _batteryService.GetBatteryHolderByGodparentUserIdAsync(authenticatedUserId);
                return Ok(holder);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        /// <summary>
        /// Retrieves information about the authenticated godparents battery.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains an 
        /// <see cref="ActionResult"/> of type <see cref="BatteryDto.ViewBattery"/>, which is the list of all batteries.
        /// </returns>
        /// <response code="200">Returns the list of batteries.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPost("godparent/{userId}/{batteryId}/claim")]
        [Authorize(Roles = "BUUTAgent")]
        public async Task<ActionResult<UserDto.UserContactDetails>> ClaimBatteryAsGodparent(string userId, string batteryId)
        {
            try
            {
                // Get the authenticated user's ID if non existent throw error
                var claim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (claim == null)
                {
                    throw new InvalidOperationException("User is not authenticated or NameIdentifier claim is missing.");
                }
                string authenticatedUserId = claim.Value;
                if (userId != userId)
                {
                    throw new InvalidOperationException("Authenticated user and requested user do not match");
                }

                // get the childbattery of the user
                var holder = await _batteryService.ClaimBatteryAsGodparentAsync(authenticatedUserId, batteryId);

                return Ok(holder);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error occurred during Claiming of the battery");
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        /// <summary>
        /// Updates the details of an existing battery by its ID.
        /// </summary>
        /// <param name="id">The ID of the battery to be updated.</param>
        /// <param name="battery">The updated battery details.</param>
        /// <returns>
        /// A task representing the asynchronous operation. The result is an <see cref="ActionResult"/>:
        /// <list type="bullet">
        ///     <item><description><see cref="NoContentResult"/> if the battery was successfully updated.</description></item>
        ///     <item><description><see cref="BadRequestResult"/> if the provided ID or battery details are invalid.</description></item>
        ///     <item><description><see cref="NotFoundResult"/> if no battery was found with the provided ID.</description></item>
        ///     <item><description><see cref="StatusCodeResult"/> (500) if an error occurred during the update process.</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// This method performs an update operation on the battery identified by the provided ID.
        /// If the provided ID does not match the ID in the request body, or if the battery details are invalid,
        /// a BadRequest status is returned. If the battery is successfully updated, a NoContent response is returned.
        /// If the battery cannot be found for the given ID, a NotFound status is returned.
        /// </remarks>
        /// <exception cref="Exception">Thrown if an error occurs during the update process.</exception>
        [HttpPut("{id}")]
        public async Task<ActionResult> Put(string id, [FromBody] BatteryDto.UpdateBattery? battery)
        {
            if (string.IsNullOrWhiteSpace(id) || battery is null || battery.id != id)
            {
                _logger.LogWarning("Invalid battery ID or details provided for update.");
                return BadRequest("Invalid battery ID or details.");
            }

            try
            {

                var updated = await _batteryService.UpdateAsync(battery);
                _logger.LogInformation("Updating item with ID {Id}, New Name: {Name}", battery.id, battery.name);
                if (updated)
                {

                    _logger.LogInformation("battery with ID {battery} updated successfully.", id);
                    return NoContent(); // Explicitly return NoContentResult
                }

                _logger.LogWarning("battery with ID {batteryId} update failed.", id);
                return NotFound($"battery with ID '{id}' was not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating battery with ID {battery}.", id);
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
                _logger.LogWarning("Invalid battery ID provided for deletion.");
                return BadRequest("battery ID cannot be null or empty.");
            }

            try
            {
                var deleted = await _batteryService.DeleteAsync(id);
                if (deleted)
                {
                    _logger.LogInformation("battery with ID {batteryId} deleted successfully.", id);
                    return NoContent(); // Explicitly return NoContentResult
                }

                _logger.LogWarning("battery with ID {batteryId} deletion failed.", id);
                return NotFound($"battery with ID '{id}' was not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting battery with ID {battery}.", id);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

    }
}
