

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rise.Server.Controllers;
using Rise.Shared.Boats;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class BoatController : ControllerBase
{
    private readonly IEquipmentService<BoatDto.ViewBoat, BoatDto.NewBoat, BoatDto.UpdateBoat> _boatService;
    private readonly ILogger<BoatController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoatController"/> class.
    /// </summary>
    /// <param name="boatService">The service to manage boat-related operations.</param>
    public BoatController(IEquipmentService<BoatDto.ViewBoat, BoatDto.NewBoat, BoatDto.UpdateBoat> boatService, ILogger<BoatController> logger)
    {
        _boatService = boatService;
        _logger = logger;
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
            _logger.LogInformation("Successfully retrieved all boats.");
            return Ok(boats);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while retrieving all boats.");
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
            _logger.LogWarning("Boat details cannot be null.");
            return BadRequest("Boat details can't be null");
        }
        try
        {
            var createdBoat = await _boatService.CreateAsync(boat);
            _logger.LogInformation("Boat successfully created with ID {BoatId}.", createdBoat.boatId);
            return CreatedAtAction(null, null, createdBoat);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    /// <summary>
    /// Updates the details of an existing boat by its ID.
    /// </summary>
    /// <param name="id">The ID of the boat to be updated.</param>
    /// <param name="boat">The updated boat details.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The result is an <see cref="ActionResult"/>:
    /// <list type="bullet">
    ///     <item><description><see cref="NoContentResult"/> if the boat was successfully updated.</description></item>
    ///     <item><description><see cref="BadRequestResult"/> if the provided ID or boat details are invalid.</description></item>
    ///     <item><description><see cref="NotFoundResult"/> if no boat was found with the provided ID.</description></item>
    ///     <item><description><see cref="StatusCodeResult"/> (500) if an error occurred during the update process.</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// This method performs an update operation on the boat identified by the provided ID.
    /// If the provided ID does not match the ID in the request body, or if the boat details are invalid,
    /// a BadRequest status is returned. If the boat is successfully updated, a NoContent response is returned.
    /// If the boat cannot be found for the given ID, a NotFound status is returned.
    /// </remarks>
    /// <exception cref="Exception">Thrown if an error occurs during the update process.</exception>
    [HttpPut("{id}")]
    public async Task<ActionResult> Put(string id, [FromBody] BoatDto.UpdateBoat? boat)
    {
        if (string.IsNullOrWhiteSpace(id) || boat is null || boat.id != id)
        {
            _logger.LogWarning("Invalid boat ID or details provided for update.");
            return BadRequest("Invalid boat ID or details.");
        }

        try
        {

            var updated = await _boatService.UpdateAsync(boat);
            _logger.LogInformation("Updating item with ID {Id}, New Name: {Name}", boat.id, boat.name);
            if (updated)
            {

                _logger.LogInformation("Boat with ID {Boat} updated successfully.", id);
                return NoContent(); // Explicitly return NoContentResult
            }

            _logger.LogWarning("Boat with ID {BoatId} update failed.", id);
            return NotFound($"Boat with ID '{id}' was not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating boat with ID {Boat}.", id);
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
            _logger.LogWarning("Invalid boat ID provided for deletion.");
            return BadRequest("Boat ID cannot be null or empty.");
        }

        try
        {
            var deleted = await _boatService.DeleteAsync(id);
            if (deleted)
            {
                _logger.LogInformation("Boat with ID {BoatId} deleted successfully.", id);
                return NoContent(); // Explicitly return NoContentResult
            }

            _logger.LogWarning("Boat with ID {BoatId} deletion failed.", id);
            return NotFound($"Boat with ID '{id}' was not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting boat with ID {Boat}.", id);
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }
}