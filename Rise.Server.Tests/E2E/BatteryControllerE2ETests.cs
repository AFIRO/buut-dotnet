using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rise.Domain.Bookings;
using Rise.Domain.Users;
using Rise.Persistence;
using Rise.Server.Tests.E2E;
using Rise.Shared.Bookings;
using Rise.Shared.Enums;
using Rise.Shared.Users;
using Shouldly;

[Collection("IntegrationTests")]
public class BatteryControllerE2ETests : BaseControllerE2ETests
{

    private readonly string _baseAddress = "/api/battery";
    private readonly string _buutAgentAuth0Id = "auth0|6713ad784fda04f4b9ae2165";
    private readonly string _userAuth0Id = "auth0|6713ad614fda04f4b9ae2156";
    private readonly string _adminAuth0Id = "auth0|6713ad524e8a8907fbf0d57f";
    private readonly string _pendingAuth0Id = "auth0|6713adbf2d2a7c11375ac64c";

    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public BatteryControllerE2ETests(CustomWebApplicationFactory<Program> factory) : base(factory) {
        _jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // Hiermee maak je de JSON case-insensitive
            };
        _jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); // String-based enum deserialisatie
     }

    protected override void SeedData()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        SeedUsers(dbContext);
        SeedBatteries(dbContext);
        SeedBatteryWithBuutagent(dbContext);
    }

    private void SeedBatteryWithBuutagent(ApplicationDbContext dbContext)
    {
        Battery battery = new Battery("BatteryTestSeedWithBuutagent");
        battery.SetBatteryBuutAgent(dbContext.Users.First(u => u.Id == _buutAgentAuth0Id));
        battery.ChangeCurrentUser(dbContext.Users.First(u => u.Id == _adminAuth0Id));

        
        dbContext.Batteries.Add(battery);
        dbContext.SaveChanges();
    }

    private void SeedBatteries(ApplicationDbContext dbContext)
    {
        for (int i = 1; i <= 9; i++)
        {
            dbContext.Batteries.Add(new Battery($"BatteryTestSeed{i}"));
        }
        dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetAllAsync_AsUser_ShouldReturnForbidden()
    {
        // Arrange
        var token = Factory.GenerateJwtToken("User", "User", _userAuth0Id);
        var request = new HttpRequestMessage(HttpMethod.Get, _baseAddress);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);


        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAllAsync_AsBuutAgent_ShouldReturnForbidden()
    {
        // Arrange
        var token = Factory.GenerateJwtToken("BUUTAgent", "BUUTAgent", _buutAgentAuth0Id);
        var request = new HttpRequestMessage(HttpMethod.Get, _baseAddress);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);


        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAllAsync_WithBadCredentials_ShouldReturnForbidden()
    {
        // Arrange
        var token = Factory.GenerateJwtToken("bla", "bla", _buutAgentAuth0Id);
        var request = new HttpRequestMessage(HttpMethod.Get, _baseAddress);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);


        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAllAsync_WithBadJWTCredentialsIssuer_ShouldReturnForbidden()
    {
        // Arrange
        var token = Factory.GenerateJwtToken("Admin", "Admin", _adminAuth0Id, true);
        var request = new HttpRequestMessage(HttpMethod.Get, _baseAddress);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);


        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllAsync_WithBadJWTCredentialsIssuerAndAudience_ShouldReturnForbidden()
    {
        // Arrange
        var token = Factory.GenerateJwtToken("Admin", "Admin", _adminAuth0Id, true, true);
        var request = new HttpRequestMessage(HttpMethod.Get, _baseAddress);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);


        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllAsync_WithBadJWTCredentialsAudience_ShouldReturnForbidden()
    {
        // Arrange
        var token = Factory.GenerateJwtToken("Admin", "Admin", _adminAuth0Id,false , true);
        var request = new HttpRequestMessage(HttpMethod.Get, _baseAddress);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);


        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllAsync_AsNonLoggedIn_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, _baseAddress);

        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllAsync_AsAdmin_ShouldReturnAllBatteries()
    {
        // Arrange
        var token = Factory.GenerateJwtToken("Admin", "Admin", _adminAuth0Id);
        var request = new HttpRequestMessage(HttpMethod.Get, _baseAddress);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);


        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var batteries = await response.Content.ReadFromJsonAsync<IEnumerable<BatteryDto.ViewBattery>>();
        Assert.Equal(10, batteries?.Count());
    }

    [Fact]
    public async Task PostAsync_AsAdmin_ShouldCreateBattery()
    {
        // Arrange
        var token = Factory.GenerateJwtToken("Admin", "Admin", _adminAuth0Id);
        var request = new HttpRequestMessage(HttpMethod.Post, _baseAddress);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new BatteryDto.NewBattery { name = "NewBattery" });

        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var battery = await response.Content.ReadFromJsonAsync<BatteryDto.ViewBattery>();
        Assert.Equal("NewBattery", battery?.name);
    }

    [Fact]
    public async Task PostAsync_AsAdminBatteryNameExists_ShouldNotCreateBattery()
    {
        // Arrange Battery situation
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.Batteries.Add(new Battery("NewBattery"));
        dbContext.SaveChanges();
        
        // Arrange Authorization
        var token = Factory.GenerateJwtToken("Admin", "Admin", _adminAuth0Id);
        var request = new HttpRequestMessage(HttpMethod.Post, _baseAddress);

        // Arrange Request
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new BatteryDto.NewBattery { name = "NewBattery" });


        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        dbContext.Batteries.Count(b => b.Name == "NewBattery").ShouldBe(1);
    }

    [Fact]
    public async Task PostAsync_AsBuutAgent_ShouldNotCreateBatteryAndGiveForbidden()
    {
        // setup db
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Arrange
        var token = Factory.GenerateJwtToken("BUUTAgent", "BUUTAgent", _buutAgentAuth0Id);
        var request = new HttpRequestMessage(HttpMethod.Post, _baseAddress);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new BatteryDto.NewBattery { name = "NewBattery" });

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        dbContext.Batteries.Count(b => b.Name == "NewBattery").ShouldBe(0);
    }

    [Fact]
    public async Task PostAsync_AsUser_ShouldNotCreateBatteryAndGiveForbidden()
    {
        // setup db
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, _baseAddress);

        request.Headers.Authorization = GenerateAuthHeader("User", _userAuth0Id);
        request.Content = JsonContent.Create(new BatteryDto.NewBattery { name = "NewBattery" });

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        dbContext.Batteries.Count(b => b.Name == "NewBattery").ShouldBe(0);
    }

    [Fact]
    public async Task PostAsync_AsPendingUser_ShouldNotCreateBatteryAndGiveForbidden()
    {
        // setup db
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, _baseAddress);

        request.Headers.Authorization = GenerateAuthHeader("Pending", _userAuth0Id);
        request.Content = JsonContent.Create(new BatteryDto.NewBattery { name = "NewBattery" });

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        dbContext.Batteries.Count(b => b.Name == "NewBattery").ShouldBe(0);
    }

    [Fact]
    public async Task PostAsync_withBadHeader_ShouldNotCreateBatteryAndUnauthorized()
    {
        // setup db
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, _baseAddress);

        request.Headers.Authorization = GenerateBadAuthHeader("Admin", _adminAuth0Id);
        request.Content = JsonContent.Create(new BatteryDto.NewBattery { name = "NewBattery" });

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        dbContext.Batteries.Count(b => b.Name == "NewBattery").ShouldBe(0);
    }

    [Fact]
    public async Task PostAsync_withOutHeader_ShouldNotCreateBatteryAndUnauthorized()
    {
        // setup db
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, _baseAddress);

        request.Content = JsonContent.Create(new BatteryDto.NewBattery { name = "NewBattery" });

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        dbContext.Batteries.Count(b => b.Name == "NewBattery").ShouldBe(0);
    }

    [Fact]
    public async Task GetGodchildBattery_AsBuutAgent_ShouldReturnBattery()
    {
        // Arrange

        var request = new HttpRequestMessage(HttpMethod.Get, _baseAddress + "/godparent/info");

        request.Headers.Authorization = GenerateAuthHeader("BUUTAgent", _buutAgentAuth0Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        BatteryDto.ViewBatteryBuutAgent battery = await response.Content.ReadFromJsonAsync<BatteryDto.ViewBatteryBuutAgent>();

        Assert.NotNull(battery);
        Assert.Equal("BatteryTestSeedWithBuutagent", battery?.name);
    }

    [Fact]
    public async Task GetGodchildBattery_AsAdmin_ShouldForbidden()
    {
        // Arrange

        var request = new HttpRequestMessage(HttpMethod.Get, _baseAddress + "/godparent/info");

        request.Headers.Authorization = GenerateAuthHeader("Admin", _adminAuth0Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetGodchildBattery_AsUser_ShouldForbidden()
    {
        // Arrange

        var request = new HttpRequestMessage(HttpMethod.Get, _baseAddress + "/godparent/info");

        request.Headers.Authorization = GenerateAuthHeader("User", _userAuth0Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetGodchildBattery_AsPending_ShouldForbidden()
    {
        // Arrange

        var request = new HttpRequestMessage(HttpMethod.Get, _baseAddress + "/godparent/info");

        request.Headers.Authorization = GenerateAuthHeader("Pending", _pendingAuth0Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetGodchildBatteryHolder_AsBuutAgent_ShouldReturnBattery()
    {
        // Arrange

        var request = new HttpRequestMessage(HttpMethod.Get, _baseAddress + "/godparent/holder");

        request.Headers.Authorization = GenerateAuthHeader("BUUTAgent", _buutAgentAuth0Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Deserialize (StreetEnum geeft problemen met de standaard deserialisatie)
        var jsonStream = await response.Content.ReadAsStreamAsync();
        UserDto.UserContactDetails holder = await JsonSerializer.DeserializeAsync<UserDto.UserContactDetails>(jsonStream, _jsonSerializerOptions);

        Assert.NotNull(holder);
        Assert.Equal("Admin", holder?.FirstName);
        Assert.Equal("Gebruiker", holder?.LastName);
        Assert.Equal(_adminAuth0Id, holder?.Id);
    }

    [Fact]
    public async Task GetGodchildBatteryHolder_AsAdmin_ShouldForbidden()
    {
        // Arrange

        var request = new HttpRequestMessage(HttpMethod.Get, _baseAddress + "/godparent/holder");

        request.Headers.Authorization = GenerateAuthHeader("Admin", _adminAuth0Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetGodchildBatteryHolder_AsUser_ShouldForbidden()
    {
        // Arrange

        var request = new HttpRequestMessage(HttpMethod.Get, _baseAddress + "/godparent/holder");

        request.Headers.Authorization = GenerateAuthHeader("User", _userAuth0Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetGodchildBatteryHolder_AsPending_ShouldForbidden()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, _baseAddress + "/godparent/holder");
        request.Headers.Authorization = GenerateAuthHeader("Pending", _pendingAuth0Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }


    [Fact]
    public async Task PostClaimBatteryAsGodparent_asCorrectBuutAgent_ShouldClaimBattery()
    {
        
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries
        .Include(b => b.BatteryBuutAgent)
        .Include(b => b.CurrentUser)
        .First(b => b.Name == "BatteryTestSeedWithBuutagent");
        
        User startHolder = battery.CurrentUser;
        User buutAgent = battery.BatteryBuutAgent;
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, _baseAddress + "/godparent/" + _buutAgentAuth0Id + "/" + battery.Id + "/claim");
        request.Headers.Authorization = GenerateAuthHeader("BUUTAgent", _buutAgentAuth0Id);

        // Act
        var response = await Client.SendAsync(request);
        // Deserialize (StreetEnum geeft problemen met de standaard deserialisatie)
        var jsonStream = await response.Content.ReadAsStreamAsync();
        UserDto.UserContactDetails newHolder = await JsonSerializer.DeserializeAsync<UserDto.UserContactDetails>(jsonStream, _jsonSerializerOptions)!;

        // Assert 
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(newHolder.Id, _buutAgentAuth0Id);
        Assert.Equal(newHolder.FirstName, buutAgent.FirstName);
        Assert.Equal(newHolder.LastName, buutAgent.LastName);
        Assert.Equal(newHolder.Email, buutAgent.Email);
        Assert.Equal(newHolder.PhoneNumber, buutAgent.PhoneNumber);
    }

    [Fact]
    public async Task PostClaimBatteryAsGodparent_asCorrectBuutAgentWithWrongGivenId_ShouldReturnBadRequest()
    {
        
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries
        .Include(b => b.BatteryBuutAgent)
        .Include(b => b.CurrentUser)
        .First(b => b.Name == "BatteryTestSeedWithBuutagent");
        
        User startHolder = battery.CurrentUser;
        User buutAgent = battery.BatteryBuutAgent;
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, _baseAddress + "/godparent/" + _pendingAuth0Id + "/" + battery.Id + "/claim");
        request.Headers.Authorization = GenerateAuthHeader("BUUTAgent", _buutAgentAuth0Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostClaimBatteryAsGodparent_asCorrectBuutAgentWithNonExistingBatteryId_ShouldReturnBadRequest()
    {
        
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, _baseAddress + "/godparent/" + _buutAgentAuth0Id + "/" + "badId" + "/claim");
        request.Headers.Authorization = GenerateAuthHeader("BUUTAgent", _buutAgentAuth0Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostClaimBatteryAsGodparent_asCorrectBuutAgentWithWrongBatteryId_ShouldReturnBadRequest()
    {
        
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries
        .Include(b => b.BatteryBuutAgent)
        .Include(b => b.CurrentUser)
        .First(b => b.Name == "BatteryTestSeed1");
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, _baseAddress + "/godparent/" + _buutAgentAuth0Id + "/" + battery.Id + "/claim");
        request.Headers.Authorization = GenerateAuthHeader("BUUTAgent", _buutAgentAuth0Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostClaimBatteryAsGodparent_asAdmin_ShouldReturnForbidden()
    {
        
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries
        .Include(b => b.BatteryBuutAgent)
        .Include(b => b.CurrentUser)
        .First(b => b.Name == "BatteryTestSeedWithBuutagent");
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, _baseAddress + "/godparent/" + _buutAgentAuth0Id + "/" + battery.Id + "/claim");
        request.Headers.Authorization = GenerateAuthHeader("Admin", _adminAuth0Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostClaimBatteryAsGodparent_asUser_ShouldReturnForbidden()
    {
        
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries
        .Include(b => b.BatteryBuutAgent)
        .Include(b => b.CurrentUser)
        .First(b => b.Name == "BatteryTestSeedWithBuutagent");
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, _baseAddress + "/godparent/" + _buutAgentAuth0Id + "/" + battery.Id + "/claim");
        request.Headers.Authorization = GenerateAuthHeader("Admin", _adminAuth0Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

        [Fact]
    public async Task PostClaimBatteryAsGodparent_asPending_ShouldReturnForbidden()
    {
        
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries
        .Include(b => b.BatteryBuutAgent)
        .Include(b => b.CurrentUser)
        .First(b => b.Name == "BatteryTestSeedWithBuutagent");
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, _baseAddress + "/godparent/" + _buutAgentAuth0Id + "/" + battery.Id + "/claim");
        request.Headers.Authorization = GenerateAuthHeader("Pending", _pendingAuth0Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostClaimBatteryAsGodparent_asBuutAgentWithBadJwt_ShouldReturnForbidden()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries
        .Include(b => b.BatteryBuutAgent)
        .Include(b => b.CurrentUser)
        .First(b => b.Name == "BatteryTestSeedWithBuutagent");
        
        User startHolder = battery.CurrentUser;
        User buutAgent = battery.BatteryBuutAgent;
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, _baseAddress + "/godparent/" + _buutAgentAuth0Id + "/" + battery.Id + "/claim");
        request.Headers.Authorization = GenerateBadAuthHeader("BUUTAgent", _buutAgentAuth0Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostClaimBatteryAsGodparent_asBuutAgentWithoutAuthHeader_ShouldReturnForbidden()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries
        .Include(b => b.BatteryBuutAgent)
        .Include(b => b.CurrentUser)
        .First(b => b.Name == "BatteryTestSeedWithBuutagent");
        
        User startHolder = battery.CurrentUser;
        User buutAgent = battery.BatteryBuutAgent;
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, _baseAddress + "/godparent/" + _buutAgentAuth0Id + "/" + battery.Id + "/claim");

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutBatteryAsAdmin_ShouldUpdateBattery()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries
        .Include(b => b.BatteryBuutAgent)
        .Include(b => b.CurrentUser)
        .First(b => b.Name == "BatteryTestSeedWithBuutagent");
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, _baseAddress + "/" + battery.Id);
        request.Headers.Authorization = GenerateAuthHeader("Admin", _adminAuth0Id);
        request.Content = JsonContent.Create(
            new BatteryDto.UpdateBattery {
                id= battery.Id, 
                name = "NewBatteryName"
                }
            );

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        dbContext.Batteries.Count(b => b.Name == "NewBatteryName").ShouldBe(1);
        dbContext.Batteries.Count(b => b.Name == "BatteryTestSeedWithBuutagent").ShouldBe(0);
    }

    [Fact]
    public async Task PutBatteryAsBuutAgent_ShouldUpdateBattery()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries
        .Include(b => b.BatteryBuutAgent)
        .Include(b => b.CurrentUser)
        .First(b => b.Name == "BatteryTestSeedWithBuutagent");
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, _baseAddress + "/" + battery.Id);
        request.Headers.Authorization = GenerateAuthHeader("BUUTAgent", _buutAgentAuth0Id);
        request.Content = JsonContent.Create(
            new BatteryDto.UpdateBattery {
                id= battery.Id, 
                name = "NewBatteryName"
                }
            );

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        dbContext.Batteries.Count(b => b.Name == "NewBatteryName").ShouldBe(1);
        dbContext.Batteries.Count(b => b.Name == "BatteryTestSeedWithBuutagent").ShouldBe(0);
    }

    [Fact]
    public async Task PutBatteryAsUser_ShouldForbiddenAndNotUpdateBattery()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries
        .Include(b => b.BatteryBuutAgent)
        .Include(b => b.CurrentUser)
        .First(b => b.Name == "BatteryTestSeedWithBuutagent");
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, _baseAddress + "/" + battery.Id);
        request.Headers.Authorization = GenerateAuthHeader("User", _userAuth0Id);
        request.Content = JsonContent.Create(
            new BatteryDto.UpdateBattery {
                id= battery.Id, 
                name = "NewBatteryName"
                }
            );

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        dbContext.Batteries.Count(b => b.Name == "NewBatteryName").ShouldBe(0);
        dbContext.Batteries.Count(b => b.Name == "BatteryTestSeedWithBuutagent").ShouldBe(1);
    }

    [Fact]
    public async Task PutBatteryAsPending_ShouldForbiddenAndNotUpdateBattery()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries
        .Include(b => b.BatteryBuutAgent)
        .Include(b => b.CurrentUser)
        .First(b => b.Name == "BatteryTestSeedWithBuutagent");
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, _baseAddress + "/" + battery.Id);
        request.Headers.Authorization = GenerateAuthHeader("Pending", _pendingAuth0Id);
        request.Content = JsonContent.Create(
            new BatteryDto.UpdateBattery {
                id= battery.Id, 
                name = "NewBatteryName"
                }
    );

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        dbContext.Batteries.Count(b => b.Name == "NewBatteryName").ShouldBe(0);
        dbContext.Batteries.Count(b => b.Name == "BatteryTestSeedWithBuutagent").ShouldBe(1);
    }

    [Fact]
    public async Task PutBatteryWithoutHeader_ShouldUnauthorizedAndNotUpdateBattery()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries
        .Include(b => b.BatteryBuutAgent)
        .Include(b => b.CurrentUser)
        .First(b => b.Name == "BatteryTestSeedWithBuutagent");
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, _baseAddress + "/" + battery.Id);
        request.Content = JsonContent.Create(
            new BatteryDto.UpdateBattery {
                id= battery.Id, 
                name = "NewBatteryName"
                }
            );

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        dbContext.Batteries.Count(b => b.Name == "NewBatteryName").ShouldBe(0);
        dbContext.Batteries.Count(b => b.Name == "BatteryTestSeedWithBuutagent").ShouldBe(1);
    }

    [Fact]
    public async Task PutBatteryAsUserWithBadHeader_ShouldUnauthorizedAndNotUpdateBattery()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries
        .Include(b => b.BatteryBuutAgent)
        .Include(b => b.CurrentUser)
        .First(b => b.Name == "BatteryTestSeedWithBuutagent");
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, _baseAddress + "/" + battery.Id);
        request.Headers.Authorization = GenerateBadAuthHeader("User", _userAuth0Id);
        request.Content = JsonContent.Create(
            new BatteryDto.UpdateBattery {
                id= battery.Id, 
                name = "NewBatteryName"
                }
            );

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        dbContext.Batteries.Count(b => b.Name == "NewBatteryName").ShouldBe(0);
        dbContext.Batteries.Count(b => b.Name == "BatteryTestSeedWithBuutagent").ShouldBe(1);
    }

    [Fact]
    public async Task PutBatteryAsAdminWithBadHeader_ShouldUnauthorizedAndNotUpdateBattery()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries
        .Include(b => b.BatteryBuutAgent)
        .Include(b => b.CurrentUser)
        .First(b => b.Name == "BatteryTestSeedWithBuutagent");
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, _baseAddress + "/" + battery.Id);
        request.Headers.Authorization = GenerateBadAuthHeader("Admin", _adminAuth0Id);
        request.Content = JsonContent.Create(
            new BatteryDto.UpdateBattery {
                id= battery.Id, 
                name = "NewBatteryName"
                }
            );

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        dbContext.Batteries.Count(b => b.Name == "NewBatteryName").ShouldBe(0);
        dbContext.Batteries.Count(b => b.Name == "BatteryTestSeedWithBuutagent").ShouldBe(1);
    }

    [Fact]
    public async Task PutBatteryAsAdmin_withoutId_ShouldBadRequestAndNotUpdateBattery()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries
        .Include(b => b.BatteryBuutAgent)
        .Include(b => b.CurrentUser)
        .First(b => b.Name == "BatteryTestSeedWithBuutagent");
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, _baseAddress + "/" + battery.Id);
        request.Headers.Authorization = GenerateAuthHeader("Admin", _adminAuth0Id);
        request.Content = JsonContent.Create(
            new BatteryDto.UpdateBattery { 
                name = "NewBatteryName"
                }
            );

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        dbContext.Batteries.Count(b => b.Name == "NewBatteryName").ShouldBe(0);
        dbContext.Batteries.Count(b => b.Name == "BatteryTestSeedWithBuutagent").ShouldBe(1);
    }

    [Fact]
    public async Task PutBatteryAsAdmin_EmptyBatteryDto_ShouldBadRequestAndNotUpdateBattery()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries
        .Include(b => b.BatteryBuutAgent)
        .Include(b => b.CurrentUser)
        .First(b => b.Name == "BatteryTestSeedWithBuutagent");
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, _baseAddress + "/" + battery.Id);
        request.Headers.Authorization = GenerateAuthHeader("Admin", _adminAuth0Id);
        request.Content = JsonContent.Create(new BatteryDto.UpdateBattery { } );

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        dbContext.Batteries.Count(b => b.Name == "NewBatteryName").ShouldBe(0);
        dbContext.Batteries.Count(b => b.Name == "BatteryTestSeedWithBuutagent").ShouldBe(1);
    }

    [Fact]
    public async Task PutBatteryAsAdmin_WrongId_ShouldBadRequestAndNotUpdateBattery()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries
        .Include(b => b.BatteryBuutAgent)
        .Include(b => b.CurrentUser)
        .First(b => b.Name == "BatteryTestSeedWithBuutagent");
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, _baseAddress + "/" + battery.Id);
        request.Headers.Authorization = GenerateAuthHeader("Admin", _adminAuth0Id);
        request.Content = JsonContent.Create(
            new BatteryDto.UpdateBattery {
                id= battery.Id + "wrong", 
                name = "NewBatteryName"
                }
            );

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        dbContext.Batteries.Count(b => b.Name == "NewBatteryName").ShouldBe(0);
        dbContext.Batteries.Count(b => b.Name == "BatteryTestSeedWithBuutagent").ShouldBe(1);
    }

    [Fact]
    public async Task PutBatteryAsAdmin_GivingUserDto_ShouldBadRequestAndNotUpdateBattery()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries
        .Include(b => b.BatteryBuutAgent)
        .Include(b => b.CurrentUser)
        .First(b => b.Name == "BatteryTestSeedWithBuutagent");
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, _baseAddress + "/" + battery.Id);
        request.Headers.Authorization = GenerateAuthHeader("Admin", _adminAuth0Id);
        request.Content = JsonContent.Create(
            new UserDto.UserContactDetails {
                Id = battery.BatteryBuutAgent.Id,
                FirstName = "NewFirstName",
                LastName = "NewLastName",
                Email = "NewEmail",
                PhoneNumber = "NewPhoneNumber",
                Address = new AddressDto.CreateAddress
                {
                    Street = StreetEnum.AFRIKALAAN,
                    HouseNumber = "NewNumber",
                    Bus = "NewBus",

                }}
            );

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        dbContext.Batteries.Count(b => b.Name == "NewBatteryName").ShouldBe(0);
        dbContext.Batteries.Count(b => b.Name == "BatteryTestSeedWithBuutagent").ShouldBe(1);
    }

    [Fact]
    public async Task DeleteBattery_AsAdmin_ShouldDeleteBattery()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries.First(b => b.Name == "BatteryTestSeed1");
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, _baseAddress + "/" + battery.Id);
        request.Headers.Authorization = GenerateAuthHeader("Admin", _adminAuth0Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        dbContext.Batteries.Count(b => b.Name == "BatteryTestSeed1" && b.IsDeleted == true).ShouldBe(1);
    }

    [Fact]
    public async Task DeleteBattery_AsBuutAgent_ShouldDeleteBattery()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries.First(b => b.Name == "BatteryTestSeed1");
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, _baseAddress + "/" + battery.Id);
        request.Headers.Authorization = GenerateAuthHeader("BUUTAgent", _buutAgentAuth0Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert 
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        dbContext.Batteries.Count(b => b.Name == "BatteryTestSeed1" && b.IsDeleted == true).ShouldBe(1);
        dbContext.Batteries.Count(b => b.Name == "BatteryTestSeed1" && b.IsDeleted == false).ShouldBe(0);
    }

    [Fact]
    public async Task DeleteBattery_AsAdminWithNonExistingId_ShouldNotFound()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries.First(b => b.Name == "BatteryTestSeed1");
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, _baseAddress + "/" + battery.Id + "wrong");
        request.Headers.Authorization = GenerateAuthHeader("Admin", _adminAuth0Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        dbContext.Batteries.Count(b => b.Name == "BatteryTestSeed1" && b.IsDeleted == false).ShouldBe(1);
    }

    [Fact]
    public async Task DeleteBattery_AsUser_ShouldForbidden()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries.First(b => b.Name == "BatteryTestSeed1");
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, _baseAddress + "/" + battery.Id);
        request.Headers.Authorization = GenerateAuthHeader("User", _userAuth0Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        dbContext.Batteries.Count(b => b.Name == "BatteryTestSeed1" && b.IsDeleted == true).ShouldBe(0);
        dbContext.Batteries.Count(b => b.Name == "BatteryTestSeed1" && b.IsDeleted == false).ShouldBe(1);
    }

    [Fact]
    public async Task DeleteBattery_AsPending_ShouldForbidden()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries.First(b => b.Name == "BatteryTestSeed1");
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, _baseAddress + "/" + battery.Id);
        request.Headers.Authorization = GenerateAuthHeader("Pending", _pendingAuth0Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        dbContext.Batteries.Count(b => b.Name == "BatteryTestSeed1" && b.IsDeleted == true).ShouldBe(0);
        dbContext.Batteries.Count(b => b.Name == "BatteryTestSeed1" && b.IsDeleted == false).ShouldBe(1);
    }

    [Fact]
    public async Task DeleteBattery_AsAdminWithBadJWT_ShouldUnauthorized()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries.First(b => b.Name == "BatteryTestSeed1");
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, _baseAddress + "/" + battery.Id);
        request.Headers.Authorization = GenerateBadAuthHeader("Admin", _adminAuth0Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        dbContext.Batteries.Count(b => b.Name == "BatteryTestSeed1" && b.IsDeleted == true).ShouldBe(0);
        dbContext.Batteries.Count(b => b.Name == "BatteryTestSeed1" && b.IsDeleted == false).ShouldBe(1);
    }

    [Fact]
    public async Task DeleteBattery_withoutAuthHeader_ShouldUnauthorized()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Battery battery = dbContext.Batteries.First(b => b.Name == "BatteryTestSeed1");
        
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, _baseAddress + "/" + battery.Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        dbContext.Batteries.Count(b => b.Name == "BatteryTestSeed1" && b.IsDeleted == true).ShouldBe(0);
        dbContext.Batteries.Count(b => b.Name == "BatteryTestSeed1" && b.IsDeleted == false).ShouldBe(1);
    }

    private AuthenticationHeaderValue? GenerateBadAuthHeader(string role, string id)
    {
        return new AuthenticationHeaderValue("Bearer", Factory.GenerateJwtToken(role, role, id, true, true));
    }

    private AuthenticationHeaderValue GenerateAuthHeader(string role, string id)
    {
        return new AuthenticationHeaderValue("Bearer", Factory.GenerateJwtToken(role, role, id));
    }

    private void SeedUsers(ApplicationDbContext dbContext)
    {
        // generating roles
        Role roleAdmin = new Role(RolesEnum.Admin);
        Role roleUser = new Role(RolesEnum.User);
        Role roleBUUTAgent = new Role(RolesEnum.BUUTAgent);
        Role rolePending = new Role(RolesEnum.Pending);

        var address1 = new Address("Afrikalaan", "5");
        var address2 = new Address("Bataviabrug", "35");
        var address3 = new Address("Deckerstraat", "4");
        var address4 = new Address("Deckerstraat", "6");

        // generating users
        User userAdmin = new User(_adminAuth0Id, "Admin", "Gebruiker", "admin@hogent.be",
            new DateTime(1980, 01, 01, 0, 0, 0, DateTimeKind.Utc), address1, "+32478457845");
        User userBUUTAgent = new User(_buutAgentAuth0Id, "mark", "BUUTAgent", "BUUTAgent@hogent.be",
            new DateTime(1986, 09, 27, 0, 0, 0, DateTimeKind.Utc), address2, "+32478471869");
        User userUser = new User(_userAuth0Id, "User", "Gebruiker", "user@hogent.be",
            new DateTime(1990, 05, 16, 0, 0, 0, DateTimeKind.Utc), address3, "+32474771836");
        User userPending = new User(_pendingAuth0Id, "Pending", "Gebruiker", "pending@hogent.be",
            new DateTime(1990, 05, 16, 0, 0, 0, DateTimeKind.Utc), address4, "+32474771836");

        // adding roles to users
        userAdmin.Roles.Add(roleAdmin);
        userAdmin.Roles.Add(roleUser);

        userUser.Roles.Add(roleUser);

        userBUUTAgent.Roles.Add(roleBUUTAgent);
        userBUUTAgent.Roles.Add(roleUser);

        userPending.Roles.Add(rolePending);

        // adding users to the database
        dbContext.Users.AddRange(userAdmin, userUser, userBUUTAgent, userPending);
        dbContext.Roles.AddRange(roleAdmin, roleUser, roleBUUTAgent, rolePending);

        dbContext.SaveChanges();
    }
}