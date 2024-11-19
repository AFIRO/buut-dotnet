
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Rise.Persistence;
using Rise.Shared.Boats;
using Rise.Domain.Bookings;
using Microsoft.IdentityModel.Tokens;

public class BoatService : IBoatService
{
    private readonly ApplicationDbContext _dbContext;

    public BoatService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;        
    }

    public async Task<IEnumerable<BoatDto.ViewBoat>?> GetAllAsync()
    {
        var query = await _dbContext.Boats.ToListAsync();  
        return query.IsNullOrEmpty() ? null  : query.Select(MapToDto);
              
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