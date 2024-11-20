using Microsoft.EntityFrameworkCore;
using Xunit;
using Rise.Domain.Bookings;
using Rise.Persistence;
using Moq;
using Rise.Shared.Services;
using Rise.Shared.Boats;

namespace Rise.Services.Tests.Bookings;

public class BoatServiceTest
{
    private readonly ApplicationDbContext _dbContext;
    private readonly BoatService _boatService;
    private readonly Mock<IValidationService> _validationServiceMock;

    public BoatServiceTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _validationServiceMock = new Mock<IValidationService>();
        _boatService = new BoatService(_dbContext, _validationServiceMock.Object);
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

    #region CreateBoatAsync

    [Fact]
    public async Task CreateBoatAsync_WithValidName_ShouldCreateBoat()
    {
        //Arrange
        var newBoat = new BoatDto.NewBoat{name = "NewBoat"};
        _validationServiceMock.Setup(service => service.BoatExists(newBoat.name)).ReturnsAsync(false);

        //Act
        var result = await _boatService.CreateBoatAsync(newBoat);

        //Assert
        Assert.Equal(newBoat.name, result.name);

    }

    [Fact]
    public async Task CreateBoatAsync_BoatAlreadyExists_ShouldThrowException()
    {
        //Arrange
        var newBoat = new BoatDto.NewBoat{name = "NewBoat"};
        _validationServiceMock.Setup(service => service.BoatExists(newBoat.name)).ReturnsAsync(true);

        //Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _boatService.CreateBoatAsync(newBoat));       

    }

    #endregion
    

}