using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Rise.Domain.Bookings;
using Rise.Persistence;
using Rise.Shared.Bookings;
using Rise.Shared.Services;

public class BatteryService : IEquipmentService<BatteryDto.ViewBattery, BatteryDto.NewBattery>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IValidationService _validationService;

    public BatteryService(ApplicationDbContext dbContext, IValidationService validationService)
    {
        _dbContext = dbContext;       
        _validationService = validationService; 
    }

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

    public async Task<IEnumerable<BatteryDto.ViewBattery>?> GetAllAsync()
    {
        var query = await _dbContext.Batteries.ToListAsync();
        return query.IsNullOrEmpty() ? null : query.Select(MapToDto);
    }

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