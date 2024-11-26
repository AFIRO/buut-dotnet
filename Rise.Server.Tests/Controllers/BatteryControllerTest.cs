using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rise.Shared.Bookings;
using Shouldly;

public class BatteryControllerTest
{
    private readonly Mock<IEquipmentService<BatteryDto.ViewBattery, BatteryDto.NewBattery>> _mockBatteryService;
    private readonly BatteryController _controller;

    public BatteryControllerTest()
    {
        _mockBatteryService = new Mock<IEquipmentService<BatteryDto.ViewBattery, BatteryDto.NewBattery>>();        
        _controller = new BatteryController(_mockBatteryService.Object);
       
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
    public async Task Post_ValidNewBattery_ReturnsCreatedActionResult()
    {
        //Arrange
        var newBattery = new BatteryDto.NewBattery{name = "New Battery"};
        var createdBattery = new BatteryDto.ViewBattery{name = "New Battery", countBookings = 0, listComments = null};
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
        var newBattery = new BatteryDto.NewBattery{name = "New Battery"};
        _mockBatteryService.Setup(service => service.CreateAsync(newBattery)).ThrowsAsync(new InvalidOperationException());

        //Act
        var result = await _controller.Post(newBattery);

        //Assert
        var createdResult = result as ObjectResult;
        createdResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }
}