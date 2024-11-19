using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rise.Shared.Boats;
using Shouldly;

public class BoatControllerTest
{
    private readonly Mock<IBoatService> _mockBoatService;
    private readonly BoatController _controller;

    public BoatControllerTest()
    {
        _mockBoatService = new Mock<IBoatService>();        
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
}