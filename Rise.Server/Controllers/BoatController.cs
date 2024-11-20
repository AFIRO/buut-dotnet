

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rise.Server.Controllers;
using Rise.Shared.Boats;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class BoatController : ControllerBase
{
    private readonly IBoatService _boatService;

    public BoatController(IBoatService boatSerivce)
    {
        _boatService = boatSerivce;
    }

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

    [HttpPost]
    public async Task<ActionResult> Post([FromBody] BoatDto.NewBoat? boat)
    {
        if (boat == null)
        {
            return BadRequest("Boat details can't be null");
        }
        try
        {
            var createdBoat = await _boatService.CreateBoatAsync(boat);
            return CreatedAtAction(null, null, createdBoat);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }
}