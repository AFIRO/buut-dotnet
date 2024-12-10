using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Rise.Domain.Bookings;
using Rise.Persistence;
using Rise.Shared.Batteries;
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
        var query = await _dbContext.Batteries.ToListAsync();
        return query.IsNullOrEmpty() ? null : query.Select(MapToDto);
    }

    /// <summary>
    /// Maps a battery entity to a battery view DTO.
    /// </summary>
    /// <param name="battery">The battery entity to map.</param>
    /// <returns>The mapped battery view DTO.</returns>
    private BatteryDto.ViewBattery MapToDto(Battery battery)
    {
        return  new BatteryDto.ViewBattery
        {
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
        if (battery == null || battery.CurrentUser == null){return null;}

        return battery.CurrentUser?.mapToUserContactDetails();
    }

    private async Task<Battery?> getGodparentsChildBatteryAsync(string godParentId){
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
        if (batteryId == null)
            throw new InvalidOperationException("Battery ID is null");
        
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
        }else {
            throw new InvalidOperationException("The given godparent is not the godparent of this battery");
        }

        return battery.CurrentUser.mapToUserContactDetails();


    }
}