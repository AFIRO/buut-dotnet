using Microsoft.EntityFrameworkCore;
using Xunit;
using Rise.Domain.Bookings;
using Rise.Persistence;

namespace Rise.Services.Tests.Bookings;

public class BoatServiceTest
{
    private readonly ApplicationDbContext _dbContext;
    private readonly BoatService _boatService;

    public BoatServiceTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _boatService = new BoatService(_dbContext);
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_ExistingBoats_ShouldReturnBoats()
    {
        var testBoat = new Boat("TestBoat");
        await _dbContext.AddAsync(testBoat);
        await _dbContext.SaveChangesAsync();

        var result = await _boatService.GetAllAsync();

        var viewBoat = result.First();
        Assert.Equal(testBoat.Name, viewBoat.name);
    }

    [Fact]
    public async Task GetAllAsync_NoExistingBoats_ShouldReturnNull()
    {      
        var result = await _boatService.GetAllAsync();        
        Assert.Null(result);
    }

    #endregion

}