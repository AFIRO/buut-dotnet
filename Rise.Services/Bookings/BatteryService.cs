using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Rise.Domain.Bookings;
using Rise.Persistence;
using Rise.Shared.Bookings;
using Rise.Shared.Services;

/// <summary>
/// Service for managing battery-related operations.
/// </summary>
public class BatteryService : IEquipmentService<BatteryDto.ViewBattery, BatteryDto.NewBattery>
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
}