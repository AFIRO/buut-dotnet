

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rise.Server.Controllers;
using Rise.Shared.Boats;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class BoatController : ControllerBase
{
    private readonly IEquipmentService<BoatDto.ViewBoat, BoatDto.NewBoat> _boatService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoatController"/> class.
    /// </summary>
    /// <param name="boatService">The service to manage boat-related operations.</param>
    public BoatController(IEquipmentService<BoatDto.ViewBoat, BoatDto.NewBoat> boatSerivce)
    {
        _boatService = boatSerivce;
    }

    /// <summary>
    /// Retrieves all boats.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an 
    /// <see cref="ActionResult"/> of type <see cref="IEnumerable{BoatDto.ViewBoat}"/>, which is the list of all boats.
    /// </returns>
    /// <response code="200">Returns the list of boats.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BoatDto.ViewBoat>>> GetAllBoats()
    {
        try
        {
            var boats = await _boatService.GetAllAsync();
            return Ok(boats);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }


    /// <summary>
    /// Creates a new boat.
    /// </summary>
    /// <param name="boat">The details of the new boat to create.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an 
    /// <see cref="ActionResult"/> that indicates the result of the creation operation.
    /// </returns>
    /// <response code="201">Returns the created boat.</response>
    /// <response code="400">If the input boat details are null.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost]
    public async Task<ActionResult> Post([FromBody] BoatDto.NewBoat? boat)
    {
        if (boat == null)
        {
            return BadRequest("Boat details can't be null");
        }
        try
        {
            var createdBoat = await _boatService.CreateAsync(boat);
            return CreatedAtAction(null, null, createdBoat);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }
}