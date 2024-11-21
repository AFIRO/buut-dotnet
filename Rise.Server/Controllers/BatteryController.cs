

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rise.Server.Controllers;
using Rise.Shared.Bookings;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class BatteryController : ControllerBase
{
    private readonly IEquipmentService<BatteryDto.ViewBattery, BatteryDto.NewBattery> _batteryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatteryController"/> class.
    /// </summary>
    /// <param name="batteryService">The service to manage battery-related operations.</param>
    public BatteryController(IEquipmentService<BatteryDto.ViewBattery, BatteryDto.NewBattery> batteryService)
    {
        _batteryService = batteryService;
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
    public async Task<ActionResult<IEnumerable<BatteryDto.ViewBattery>>> GetAllBatteries()
    {
        try
        {
            var boats = await _batteryService.GetAllAsync();
            return Ok(boats);
        }
        catch (Exception e)
        {
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
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }
}