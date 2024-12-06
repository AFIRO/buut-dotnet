using Microsoft.EntityFrameworkCore;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="BatteryService"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="validationService">The validation service.</param>
    public BatteryService(ApplicationDbContext dbContext, IValidationService validationService)
    {
        _dbContext = dbContext;       
        _validationService = validationService; 
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


    
    public async Task<UserDto.UserContactDetails?> ClaimBatteryAsGodparentAsync(string godparentId, string batteryId)
    {
        if (batteryId == null)
            return null;
        
        Battery? battery = await _dbContext.Batteries
                .Include(battery => battery.BatteryBuutAgent)
                .ThenInclude(godparent => godparent.Address)
                .Include(battery => battery.CurrentUser)
                .FirstOrDefaultAsync(battery => battery.Id == batteryId);
        
        if (battery == null)
            {return null;}
        if (battery.BatteryBuutAgent == null)
            {return null;}
        // Check if the GodParent's ID matches the given godparentId
        if (battery.BatteryBuutAgent != null && battery.BatteryBuutAgent.Id == godparentId)
        {
            battery.CurrentUser = battery.BatteryBuutAgent;
            
            // Save the updated battery back to the database
            await _dbContext.SaveChangesAsync();
        }else {
            throw new InvalidOperationException("The given godparent is not the godparent of this battery");
        }

        return battery.CurrentUser.mapToUserContactDetails();


    }
}