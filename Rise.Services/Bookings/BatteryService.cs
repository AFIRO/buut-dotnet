using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Rise.Domain.Bookings;
using Rise.Persistence;
using Rise.Shared.Batteries;
using Rise.Shared.Boats;
using Rise.Shared.Bookings;
using Rise.Shared.Services;
using Rise.Shared.Users;

namespace Rise.Services.Batteries;
/// <summary>
/// Service for managing battery-related operations.
/// </summary>
public class BatteryService : IBatteryService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IValidationService _validationService;
    private readonly ILogger<BatteryService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatteryService"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="validationService">The validation service.</param>
    public BatteryService(ApplicationDbContext dbContext, IValidationService validationService, ILogger<BatteryService> logger)
    {
        _dbContext = dbContext;
        _validationService = validationService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new battery asynchronously.
    /// </summary>
    /// <param name="battery">The new battery DTO.</param>
    /// <returns>The created battery view DTO.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a battery with the same name already exists.</exception>
    public async Task<BatteryDto.ViewBattery> CreateAsync(BatteryDto.NewBattery battery)
    {
        if (await _validationService.BatteryExists(battery.name))
        {
            throw new InvalidOperationException("There is already a battery with this name");
        }

        var newBattery = new Battery(
            name: battery.name
        );

        var dbBattery = _dbContext.Batteries.Add(newBattery);
        await _dbContext.SaveChangesAsync();

        return MapToDto(dbBattery.Entity);
    }

    /// <summary>
    /// Retrieves all batteries asynchronously.
    /// </summary>
    /// <returns>A collection of battery view DTOs, or null if no batteries are found.</returns>
    public async Task<IEnumerable<BatteryDto.ViewBattery>?> GetAllAsync()
    {
        var query = await _dbContext.Batteries.Where(x => x.IsDeleted == false).ToListAsync();
        return query.IsNullOrEmpty() ? null : query.Select(MapToDto);
    }

    /// <summary>
    /// Maps a battery entity to a battery view DTO.
    /// </summary>
    /// <param name="battery">The battery entity to map.</param>
    /// <returns>The mapped battery view DTO.</returns>
    private BatteryDto.ViewBattery MapToDto(Battery battery)
    {
        return new BatteryDto.ViewBattery
        {
            batteryId = battery.Id,
            name = battery.Name,
            countBookings = battery.CountBookings,
            listComments = battery.ListComments
        };
    }

    /// <summary>
    /// Retrieves the battery for the given godParent.
    /// </summary>
    /// <returns> battery view DTO, or null if no batterie is found.</returns>
    public async Task<BatteryDto.ViewBatteryBuutAgent> GetBatteryByGodparentUserIdAsync(string godparentId)
    {
        Battery? battery = await getGodparentsChildBatteryAsync(godparentId);
        return battery == null ? null : battery.toViewBatteryBuutAgentDto();
    }


    /// <summary>
    /// Retrieves the godparents childbatteries holder contact information.
    /// </summary>
    /// <returns>UserDto, or null if no battery is found or the battery does not have a holder.</returns>
    public async Task<UserDto.UserContactDetails?> GetBatteryHolderByGodparentUserIdAsync(string godparentId)
    {
        Battery? battery = await getGodparentsChildBatteryAsync(godparentId);
        if (battery == null || battery.CurrentUser == null) { return null; }

        return battery.CurrentUser?.mapToUserContactDetails();
    }

    private async Task<Battery?> getGodparentsChildBatteryAsync(string godParentId)
    {
        return await _dbContext.Batteries
                .Include(b => b.BatteryBuutAgent)
                .Include(b => b.CurrentUser)
                    .ThenInclude(h => h.Address) // Safely include Address of Holder
                .FirstOrDefaultAsync(b => b.BatteryBuutAgent.Id == godParentId);
    }



    /// <summary>
    /// Claims ownership of a battery as a godparent asynchronously.
    /// </summary>
    /// <param name="godparentId">The ID of the godparent.</param>
    /// <param name="batteryId">The ID of the battery.</param>
    /// <returns>The contact details of the current holder of the battery, or null if the operation fails.</returns>

    public async Task<UserDto.UserContactDetails?> ClaimBatteryAsGodparentAsync(string godparentId, string batteryId)
    {
        if (batteryId == null){
            throw new InvalidOperationException("Battery ID is null");
        }

        Battery? battery = await _dbContext.Batteries
                .Include(battery => battery.BatteryBuutAgent)
                .ThenInclude(godparent => godparent.Address)
                .Include(battery => battery.CurrentUser)
                .FirstOrDefaultAsync(battery => battery.Id == batteryId);

        if (battery == null)
            throw new InvalidOperationException("Battery (id: {batteryId}) not found in the database");
        if (battery.BatteryBuutAgent == null)
            throw new InvalidOperationException("Battery (id: {batteryId}) does not have a godparent");
        // Check if the GodParent's ID matches the given godparentId
        if (battery.BatteryBuutAgent.Id == godparentId)
        {
            // Change the current holder of the battery to the godparent
            battery.CurrentUser = battery.BatteryBuutAgent;

            // Save the updated battery back to the database
            await _dbContext.SaveChangesAsync();
        }
        else
        {
            throw new InvalidOperationException("The given godparent is not the godparent of this battery");
        }

        return battery.CurrentUser.mapToUserContactDetails();


    }

    /// <summary>
    /// Updates the details of an existing battery in the database.
    /// </summary>
    /// <param name="battery">The battery object containing the updated information.</param>
    /// <returns>
    /// A task representing the asynchronous operation, with a <see cref="bool"/> result.
    /// <c>true</c> if the battery was successfully updated; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when invalid battery details are provided.</exception>
    /// <exception cref="Exception">Thrown when an error occurs while updating the battery.</exception>
    public async Task<bool> UpdateAsync(BatteryDto.UpdateBattery battery)
    {
        try
        {
            var entity = await _dbContext.Batteries.FindAsync(battery.id) ?? throw new Exception("Battery not found");
            entity.Name = battery.name;
            _dbContext.Batteries.Update(entity);
            int response = await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Battery with ID {BatteryId} updated successfully.", battery.id);
            return response > 0;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid battery details provided.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating battery with ID {Id}.", battery.id);
            throw new Exception("Error occurred while updating battery.", ex);
        }
    }

    /// <summary>
    /// Soft deletes a battery from the database using the specified equipment ID.
    /// </summary>
    /// <param name="equipmentId">The equipment ID of the battery to be deleted.</param>
    /// <returns>
    /// A task representing the asynchronous operation, with a <see cref="bool"/> result.
    /// <c>true</c> if the battery was successfully deleted; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when an invalid battery ID is provided.</exception>
    /// <exception cref="Exception">Thrown when an error occurs while deleting the battery.</exception>
    public async Task<bool> DeleteAsync(string equipmentId)
    {
        try
        {
            var entity = await _dbContext.Batteries.FindAsync(equipmentId) ?? throw new Exception("Battery not found");

            entity.SoftDelete();
            _dbContext.Batteries.Update(entity);
            int response = await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Batteries with ID {BatteryId} deleted successfully.", equipmentId);
            return response > 0;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid battery ID provided for battery deletion.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting battery with ID {Id}.", equipmentId);
            throw new Exception("Error occurred while deleting battery.", ex);
        }
    }
}