using Microsoft.EntityFrameworkCore;
using Xunit;
using Rise.Domain.Bookings;
using Rise.Persistence;
using Moq;
using Rise.Shared.Services;
using Rise.Shared.Bookings;
using Rise.Services.Batteries;
using Microsoft.Extensions.Logging;

namespace Rise.Services.Tests.Bookings;

public class BatteryServiceTest
{
    private readonly ApplicationDbContext _dbContext;
    private readonly BatteryService _batteryService;
    private readonly Mock<IValidationService> _validationServiceMock;
    private readonly Mock<ILogger<BatteryService>> _loggerMock;

    public BatteryServiceTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _validationServiceMock = new Mock<IValidationService>();
        _loggerMock = new Mock<ILogger<BatteryService>>();
        _batteryService = new BatteryService(_dbContext, _validationServiceMock.Object, _loggerMock.Object);
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_ExistingBatteries_ShouldReturnBatteries()
    {
        var testBattery = new Battery("TestBattery");
        await _dbContext.AddAsync(testBattery);
        await _dbContext.SaveChangesAsync();

        var result = await _batteryService.GetAllAsync();

        var viewBattery = result.First();
        Assert.Equal(testBattery.Name, viewBattery.name);
    }

    [Fact]
    public async Task GetAllAsync_NoExistingBatteries_ShouldReturnNull()
    {
        var result = await _batteryService.GetAllAsync();
        Assert.Null(result);
    }

    #endregion

    #region CreateBatteryAsync

    [Fact]
    public async Task CreateAsync_WithValidName_ShouldCreateBattery()
    {
        //Arrange
        var newBattery = new BatteryDto.NewBattery { name = "NewBattery" };
        _validationServiceMock.Setup(service => service.BatteryExists(newBattery.name)).ReturnsAsync(false);

        //Act
        var result = await _batteryService.CreateAsync(newBattery);

        //Assert
        Assert.Equal(newBattery.name, result.name);

    }

    [Fact]
    public async Task CreateAsync_BatteryAlreadyExists_ShouldThrowException()
    {
        //Arrange
        var newBattery = new BatteryDto.NewBattery { name = "NewBattery" };
        _validationServiceMock.Setup(service => service.BatteryExists(newBattery.name)).ReturnsAsync(true);

        //Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _batteryService.CreateAsync(newBattery));

    }

    #endregion


}