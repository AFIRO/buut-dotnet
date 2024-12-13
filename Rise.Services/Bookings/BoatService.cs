using System.Text.Json;
using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using Rise.Persistence;
using Rise.Shared.Boats;
using Rise.Domain.Bookings;
using Microsoft.IdentityModel.Tokens;
using Rise.Shared.Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for managing boat-related operations.
/// </summary>
public class BoatService : IEquipmentService<BoatDto.ViewBoat, BoatDto.NewBoat, BoatDto.UpdateBoat>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IValidationService _validationService;
    private readonly ILogger<BoatService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoatService"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="validationService">The validation service.</param>
    public BoatService(ApplicationDbContext dbContext, IValidationService validationService, ILogger<BoatService> logger)
    {
        _validationService = validationService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all boats asynchronously.
    /// </summary>
    /// <returns>A collection of boat view DTOs, or null if no boats are found.</returns>
    public async Task<IEnumerable<BoatDto.ViewBoat>?> GetAllAsync()
    {
        var query = await _dbContext.Boats.Where(x => x.IsDeleted == false).ToListAsync();
        return query.IsNullOrEmpty() ? null : query.Select(MapToDto);
    }

    /// <summary>
    /// Creates a new boat asynchronously.
    /// </summary>
    /// <param name="boat">The new boat DTO.</param>
    /// <returns>The created boat view DTO.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a boat with the same name already exists.</exception>
    public async Task<BoatDto.ViewBoat> CreateAsync(BoatDto.NewBoat boat)
    {
        if (await _validationService.BoatExists(boat.name))
        {
            throw new InvalidOperationException("There is already a boat with this name");
        }

        var newBoat = new Boat(
            name: boat.name
        );

        var dbBoat = _dbContext.Boats.Add(newBoat);
        await _dbContext.SaveChangesAsync();

        return MapToDto(dbBoat.Entity);
    }

    /// <summary>
    /// Updates the details of an existing boat in the database.
    /// </summary>
    /// <param name="boat">The boat object containing the updated information.</param>
    /// <returns>
    /// A task representing the asynchronous operation, with a <see cref="bool"/> result.
    /// <c>true</c> if the boat was successfully updated; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when invalid boat details are provided.</exception>
    /// <exception cref="Exception">Thrown when an error occurs while updating the boat.</exception>
    public async Task<bool> UpdateAsync(BoatDto.UpdateBoat boat)
    {
        try
        {
            var entity = await _dbContext.Boats.FindAsync(boat.id) ?? throw new NotFoundException("Boat", boat.id);
            entity.Name = boat.name;
            _dbContext.Boats.Update(entity);
            int response = await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Boat with ID {BoatId} updated successfully.", boat.id);
            return response > 0;
        }
        catch (NotFoundException ex)
        {
            _logger.LogError(ex, "Boat with ID {BoatId} not found.", boat.id);
            throw;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid boat details provided.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating boat with ID {Id}.", boat.id);
            throw new Exception("Error occurred while updating boat.", ex);
        }
    }


    /// <summary>
    /// Maps a boat entity to a boat view DTO.
    /// </summary>
    /// <param name="boat">The boat entity to map.</param>
    /// <returns>The mapped boat view DTO.</returns>
    private BoatDto.ViewBoat MapToDto(Boat boat)
    {
        return new BoatDto.ViewBoat
        {
            boatId = boat.Id,
            name = boat.Name,
            countBookings = boat.CountBookings,
            listComments = boat.ListComments
        };
    }

    /// <summary>
    /// Soft deletes a boat from the database using the specified equipment ID.
    /// </summary>
    /// <param name="equipmentId">The equipment ID of the boat to be deleted.</param>
    /// <returns>
    /// A task representing the asynchronous operation, with a <see cref="bool"/> result.
    /// <c>true</c> if the boat was successfully deleted; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when an invalid boat ID is provided.</exception>
    /// <exception cref="Exception">Thrown when an error occurs while deleting the boat.</exception>
    public async Task<bool> DeleteAsync(string equipmentId)
    {
        try
        {
            var entity = await _dbContext.Boats.FindAsync(equipmentId) ?? throw new NotFoundException("BoatId", equipmentId);

            entity.SoftDelete();
            _dbContext.Boats.Update(entity);
            int response = await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Boat with ID {BoatId} deleted successfully.", equipmentId);
            return response > 0;
        }
        catch (NotFoundException ex)
        {
            _logger.LogError(ex, "Boat with ID {BoatId} not found.", equipmentId);
            throw;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid boat ID provided for boat deletion.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting boat with ID {Id}.", equipmentId);
            throw new Exception("Error occurred while deleting boat.", ex);
        }
    }

}