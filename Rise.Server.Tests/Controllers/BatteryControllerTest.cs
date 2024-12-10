using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Rise.Server.Controllers;
using Rise.Shared.Batteries;
using Rise.Shared.Bookings;
using Rise.Shared.Users;
using Shouldly;

public class BatteryControllerTest
{
    private readonly Mock<IBatteryService> _mockBatteryService;
    private readonly BatteryController _controller;
    private readonly Mock<ILogger<BatteryController>> _mockLogger;

    public BatteryControllerTest()
    {
        _mockBatteryService = new Mock<IBatteryService>();
        _mockLogger = new Mock<ILogger<BatteryController>>();

        _controller = new BatteryController(_mockBatteryService.Object, _mockLogger.Object);
    }



    [Fact]
    public async Task GetAllBatterys_WhenAdmin_ReturnsOkResult()
    {
        var Batterys = new List<BatteryDto.ViewBattery>
        {
            new BatteryDto.ViewBattery { name = "First Test Battery"},
            new BatteryDto.ViewBattery { name = "Seoncd Test Battery"}
        };
        _mockBatteryService.Setup(service => service.GetAllAsync()).ReturnsAsync(Batterys);

        var admin = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
           {
                new Claim(ClaimTypes.NameIdentifier, "auth0|12345"),
            new Claim(ClaimTypes.Role, "Admin")
           },
        "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = admin }
        };

        var result = await _controller.GetAllBatteries();

        var okResult = result.Result as OkObjectResult;
        okResult.ShouldNotBeNull();
        okResult.StatusCode.ShouldBe(StatusCodes.Status200OK);
        okResult.Value.ShouldBe(Batterys);
    }

    [Fact]
    public async Task GetAllBatteries_ReturnsInternalServerError_OnException()
    {
        // Arrange
        _mockBatteryService.Setup(s => s.GetAllAsync()).ThrowsAsync(new System.Exception("Database error"));

        // Act
        var result = await _controller.GetAllBatteries();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        Assert.Equal("Database error", statusCodeResult.Value);
    }

    [Fact]
    public async Task Post_ValidNewBattery_ReturnsCreatedActionResult()
    {
        //Arrange
        var newBattery = new BatteryDto.NewBattery { name = "New Battery" };
        var createdBattery = new BatteryDto.ViewBattery { name = "New Battery", countBookings = 0, listComments = null };
        _mockBatteryService.Setup(service => service.CreateAsync(newBattery)).ReturnsAsync(createdBattery);

        //Act
        var result = await _controller.Post(newBattery);

        //Assert
        var createdResult = result as CreatedAtActionResult;
        createdResult.StatusCode.ShouldBe(StatusCodes.Status201Created);
        createdResult.Value.ShouldBe(createdBattery);
    }

    [Fact]
    public async Task Post_NewBatteryIsNull_ReturnsBadRequest()
    {
        //Act
        var result = await _controller.Post(null);

        //Assert
        var createdResult = result as BadRequestObjectResult;
        createdResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Post_ServiceThrowsException_ReturnsInternalServerError()
    {
        //Arrange
        var newBattery = new BatteryDto.NewBattery { name = "New Battery" };
        _mockBatteryService.Setup(service => service.CreateAsync(newBattery)).ThrowsAsync(new InvalidOperationException());

        //Act
        var result = await _controller.Post(newBattery);

        //Assert
        var createdResult = result as ObjectResult;
        createdResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }
    private void SetUserClaims(string userId)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetAllBatteries_ReturnsOkWithBatteries()
    {
        // Arrange
        var mockBatteries = new List<BatteryDto.ViewBattery> { new BatteryDto.ViewBattery() };
        _mockBatteryService.Setup(s => s.GetAllAsync()).ReturnsAsync(mockBatteries);

        // Act
        var result = await _controller.GetAllBatteries();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(mockBatteries, okResult.Value);
    }

    // [Fact]
    // public async Task GetAllBatteries_ReturnsInternalServerError_OnException()
    // {
    //     // Arrange
    //     _batteryServiceMock.Setup(s => s.GetAllAsync()).ThrowsAsync(new System.Exception("Database error"));

    //     // Act
    //     var result = await _controller.GetAllBatteries();

    //     // Assert
    //     var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
    //     Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    //     Assert.Equal("Database error", statusCodeResult.Value);
    // }

    [Fact]
    public async Task Post_ValidBattery_ReturnsCreated()
    {
        // Arrange
        var newBattery = new BatteryDto.NewBattery();
        var createdBattery = new BatteryDto.ViewBattery();
        _mockBatteryService.Setup(s => s.CreateAsync(newBattery)).ReturnsAsync(createdBattery);

        // Act
        var result = await _controller.Post(newBattery);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(createdBattery, createdResult.Value);
    }

    [Fact]
    public async Task Post_NullBattery_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Post(null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Battery details can't be null", badRequestResult.Value);
    }

    [Fact]
    public async Task GetGodchildBattery_AuthenticatedUser_ReturnsBattery()
    {
        // Arrange
        var userId = "test-user-id";
        SetUserClaims(userId);

        var battery = new BatteryDto.ViewBatteryBuutAgent();
        _mockBatteryService.Setup(s => s.GetBatteryByGodparentUserIdAsync(userId)).ReturnsAsync(battery);

        // Act
        var result = await _controller.GetGodchildBattery();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(battery, okResult.Value);
    }

    [Fact]
    public async Task GetGodchildBatteryHolder_AuthenticatedUser_ReturnsHolder()
    {
        // Arrange
        var userId = "test-user-id";
        SetUserClaims(userId);

        var holder = new UserDto.UserContactDetails();
        _mockBatteryService.Setup(s => s.GetBatteryHolderByGodparentUserIdAsync(userId)).ReturnsAsync(holder);

        // Act
        var result = await _controller.GetGodchildBatteryHolder();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(holder, okResult.Value);
    }

    [Fact]
    public async Task ClaimBatteryAsGodparent_ValidClaim_ReturnsHolder()
    {
        // Arrange
        var userId = "test-user-id";
        var batteryId = "battery-id";
        SetUserClaims(userId);

        var holder = new UserDto.UserContactDetails();
        _mockBatteryService.Setup(s => s.ClaimBatteryAsGodparentAsync(userId, batteryId)).ReturnsAsync(holder);

        // Act
        var result = await _controller.ClaimBatteryAsGodparent(userId, batteryId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(holder, okResult.Value);
    }

}