

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
}