using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rise.Shared.Batteries;
using Rise.Shared.Bookings;
using Rise.Shared.Users;
using System.Security.Claims;

namespace Rise.Server.Controllers{
    /// <summary>
/// API controller for managing battery-related operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin, BUUTAgent")]
public class BatteryController : ControllerBase
{
    private readonly IBatteryService _batteryService;
    private readonly ILogger<BookingController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatteryController"/> class.
        /// </summary>
        /// <param name="batteryService">The service to manage battery-related operations.</param>
        /// <param name="logger">The logging service</param>
        public BatteryController(IBatteryService batteryService, ILogger<BookingController> logger)
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
            var boats = await _batteryService.GetAllAsync();
            return Ok(boats);
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
            if (userId != authenticatedUserId){
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
}
}
