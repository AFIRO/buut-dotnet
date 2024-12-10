using Microsoft.EntityFrameworkCore;
using Xunit;
using Rise.Domain.Bookings;
using Rise.Persistence;
using Moq;
using Rise.Shared.Services;
using Rise.Shared.Boats;
using Microsoft.Extensions.Logging;

namespace Rise.Services.Tests.Bookings;

public class BoatServiceTest
{
    private readonly ApplicationDbContext _dbContext;
    private readonly BoatService _boatService;
    private readonly Mock<IValidationService> _validationServiceMock;
    private readonly Mock<ILogger<BoatService>> _loggerMock;

    public BoatServiceTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _validationServiceMock = new Mock<IValidationService>();
        _loggerMock = new Mock<ILogger<BoatService>>();
        _boatService = new BoatService(_dbContext, _validationServiceMock.Object, _loggerMock.Object);
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
    public async Task CreateAsync_WithValidName_ShouldCreateBoat()
    {
        //Arrange
        var newBoat = new BoatDto.NewBoat { name = "NewBoat" };
        _validationServiceMock.Setup(service => service.BoatExists(newBoat.name)).ReturnsAsync(false);

        //Act
        var result = await _boatService.CreateAsync(newBoat);

        //Assert
        Assert.Equal(newBoat.name, result.name);

    }

    [Fact]
    public async Task CreateAsync_BoatAlreadyExists_ShouldThrowException()
    {
        //Arrange
        var newBoat = new BoatDto.NewBoat { name = "NewBoat" };
        _validationServiceMock.Setup(service => service.BoatExists(newBoat.name)).ReturnsAsync(true);

        //Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _boatService.CreateAsync(newBoat));

    }

    #endregion


}