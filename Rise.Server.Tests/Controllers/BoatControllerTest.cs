using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rise.Shared.Boats;
using Shouldly;

public class BoatControllerTest
{
    private readonly Mock<IEquipmentService<BoatDto.ViewBoat, BoatDto.NewBoat>> _mockBoatService;
    private readonly BoatController _controller;

    public BoatControllerTest()
    {
        _mockBoatService = new Mock<IEquipmentService<BoatDto.ViewBoat, BoatDto.NewBoat>>();        
        _controller = new BoatController(_mockBoatService.Object);
       
    }

    [Fact]
    public async Task GetAllBoats_WhenAdmin_ReturnsOkResult()
    {
        var boats = new List<BoatDto.ViewBoat>
        {
            new BoatDto.ViewBoat { name = "First Test Boat"},
            new BoatDto.ViewBoat { name = "Seoncd Test Boat"}
        };
        _mockBoatService.Setup(service => service.GetAllAsync()).ReturnsAsync(boats);

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

        var result = await _controller.GetAllBoats();

        var okResult = result.Result as OkObjectResult;
        okResult.ShouldNotBeNull();
        okResult.StatusCode.ShouldBe(StatusCodes.Status200OK);
        okResult.Value.ShouldBe(boats);
    }  

    [Fact]
    public async Task Post_ValidNewBoat_ReturnsCreatedActionResult()
    {
        //Arrange
        var newBoat = new BoatDto.NewBoat{name = "New Boat"};
        var createdBoat = new BoatDto.ViewBoat{name = "New Boat", countBookings = 0, listComments = null};
        _mockBoatService.Setup(service => service.CreateAsync(newBoat)).ReturnsAsync(createdBoat);

        //Act
        var result = await _controller.Post(newBoat);

        //Assert
        var createdResult = result as CreatedAtActionResult;
        createdResult.StatusCode.ShouldBe(StatusCodes.Status201Created);
        createdResult.Value.ShouldBe(createdBoat);        
    }

    [Fact]
    public async Task Post_NewBookingIsNull_ReturnsBadRequest()
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
        var newBoat = new BoatDto.NewBoat{name = "New Boat"};
        _mockBoatService.Setup(service => service.CreateAsync(newBoat)).ThrowsAsync(new InvalidOperationException());

        //Act
        var result = await _controller.Post(newBoat);

        //Assert
        var createdResult = result as ObjectResult;
        createdResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }
}