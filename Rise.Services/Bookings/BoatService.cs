
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Rise.Persistence;
using Rise.Shared.Boats;
using Rise.Domain.Bookings;
using Microsoft.IdentityModel.Tokens;
using Rise.Shared.Services;

public class BoatService : IEquipmentService<BoatDto.ViewBoat, BoatDto.NewBoat>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IValidationService _validationService;

   
    public BoatService(ApplicationDbContext dbContext, IValidationService validationService)
    {
        
        _validationService = validationService;
        _dbContext = dbContext;        
    }

    public async Task<IEnumerable<BoatDto.ViewBoat>?> GetAllAsync()
    {
        var query = await _dbContext.Boats.ToListAsync();  
        return query.IsNullOrEmpty() ? null  : query.Select(MapToDto);
              
    }    
    
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


    private BoatDto.ViewBoat MapToDto(Boat boat)
    {
        return new BoatDto.ViewBoat
        {
            name = boat.Name,
            countBookings = boat.CountBookings,
            listComments = boat.ListComments
        };
    }
}