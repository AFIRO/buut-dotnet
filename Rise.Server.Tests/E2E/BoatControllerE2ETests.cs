using System.Net;
using System.Net.Http.Json;
using Xunit;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Text.Json;
using System;
using System.Text.Json.Serialization;
using Rise.Domain.Bookings;
using Rise.Persistence;
using Rise.Shared.Boats;

namespace Rise.Server.Tests.E2E;

[Collection("IntegrationTests")]
public class BoatControllerE2ETests : BaseControllerE2ETests
{
    public BoatControllerE2ETests(CustomWebApplicationFactory<Program> factory) : base(factory) { }

    protected override void SeedData()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var boat1 = new Boat("boat1","Speedster", 5, new List<string> { "Great boat!", "Fast and reliable." });
        var boat2 = new Boat("boat2","WaveRider", 2, new List<string> { "Smooth ride." });
        var boat3 = new Boat("boat3","Sea Explorer", 0, new List<string>());

        dbContext.Boats.AddRange(boat1, boat2, boat3);
        dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetAllBoats_Should_Return_All_Boats()
    {
        // Arrange
        var token = GenerateJwtToken("Admin", "Admin");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/Boat");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var boats = JsonSerializer.Deserialize<IEnumerable<BoatDto.ViewBoat>>(jsonResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        });

        Assert.NotNull(boats);
        Assert.Equal(3, boats.Count()); // Ensure 3 boats are returned

        var boatList = boats.ToList();
        Assert.Contains(boatList, b => b.name == "Speedster" && b.countBookings == 5);
        Assert.Contains(boatList, b => b.name == "WaveRider" && b.countBookings == 2);
        Assert.Contains(boatList, b => b.name == "Sea Explorer" && b.countBookings == 0);
    }


    [Fact]
    public async Task CreateBoat_Should_Return_Created()
    {
        // Arrange
        var token = GenerateJwtToken("Admin", "Admin");
        var newBoat = new BoatDto.NewBoat
        {
            name = "Ocean Voyager"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/Boat")
        {
            Content = JsonContent.Create(newBoat)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
    
    [Fact]
    public async Task CreateNullBoat_Should_Return_BadRequest()
    {
        // Arrange
        var token = GenerateJwtToken("Admin", "Admin");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/Boat")
        {
            Content = JsonContent.Create(default(BoatDto.NewBoat))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("    ")]
    public async Task CreateBoatWithInvalidName_Should_Return_InternatlServerError(string name)
    {
        // Arrange
        var token = GenerateJwtToken("Admin", "Admin");
        var newBoat = new BoatDto.NewBoat
        {
            name = name
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/Boat")
        {
            Content = JsonContent.Create(newBoat)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBoat_Should_Return_NoContent()
    {
        // Arrange
        var token = GenerateJwtToken("Admin", "Admin");
        var boatId = "boat1";
        var updateBoat = new BoatDto.UpdateBoat
        {
            id = boatId,
            name = "Updated Speedster"
        };

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/Boat/{boatId}")
        {
            Content = JsonContent.Create(updateBoat)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("    ")]
    public async Task UpdateBoatWithInvalidName_Should_Return_InternatlServerError(string name)
    {
        // Arrange
        var token = GenerateJwtToken("Admin", "Admin");
        var boatId = "boat1";
        var updateBoat = new BoatDto.UpdateBoat
        {
            id = boatId,
            name = name
        };

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/Boat/{boatId}")
        {
            Content = JsonContent.Create(updateBoat)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
    
    [Fact]
    public async Task UpdateNullBoat_Should_Return_BadRequest()
    {
        // Arrange
        var token = GenerateJwtToken("Admin", "Admin");
        var boatId = "boat1";

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/Boat/{boatId}")
        {
            Content = JsonContent.Create(default(BoatDto.NewBoat))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task UpdateBoatWithInvalidId_Should_Return_NotFound()
    {
        // Arrange
        var token = GenerateJwtToken("Admin", "Admin");
        var boatId = "invalidId";
        var updateBoat = new BoatDto.UpdateBoat
        {
            id = boatId,
            name = "Titanic"
        };

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/Boat/{boatId}")
        {
            Content = JsonContent.Create(updateBoat)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBoat_Should_Return_NoContent()
    {
        // Arrange
        var token = GenerateJwtToken("Admin", "Admin");
        var boatId = "boat2";

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/Boat/{boatId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("    ")]
    public async Task DeleteBoatNullOrEmptyId_Should_Return_MethodNotAllowed(string boatId)
    {
        // Arrange
        var token = GenerateJwtToken("Admin", "Admin");

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/Boat/{boatId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }
    
    [Fact]
    public async Task DeleteBoatInvalidId_Should_Return_NotFound()
    {
        // Arrange
        var token = GenerateJwtToken("Admin", "Admin");
        var boatId = "invalidId";

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/Boat/{boatId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
