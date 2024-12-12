namespace Rise.Server.Tests.E2E;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Rise.Shared.Users;
using Rise.Persistence;
using System.Threading.Tasks;
using Rise.Shared.Enums;
using Rise.Domain.Users;
using System.Net.Http.Headers;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Net;

using Rise.Domain.Bookings;
using Auth0.Core.Exceptions;


[Collection("IntegrationTests")]
public class UserControllerE2ETests : BaseControllerE2ETests
{
    public UserControllerE2ETests(CustomWebApplicationFactory<Program> factory) : base(factory) { }

    protected override void SeedData()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // users id's
        string buutAgentAuth0Id = "auth0|6713ad784fda04f4b9ae2165";
        string userAuth0Id = "auth0|6713ad614fda04f4b9ae2156";
        string adminAuth0Id = "auth0|6713ad524e8a8907fbf0d57f";
        string pendingAuth0Id = "auth0|6713adbf2d2a7c11375ac64c";

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
        User userAdmin = new User(adminAuth0Id, "Admin", "Gebruiker", "admin@hogent.be",
            new DateTime(1980, 01, 01, 0, 0, 0, DateTimeKind.Utc), address1, "+32478457845");
        User userBUUTAgent = new User(buutAgentAuth0Id, "mark", "BUUTAgent", "BUUTAgent@hogent.be",
            new DateTime(1986, 09, 27, 0, 0, 0, DateTimeKind.Utc), address2, "+32478471869");
        User userUser = new User(userAuth0Id, "User", "Gebruiker", "user@hogent.be",
            new DateTime(1990, 05, 16, 0, 0, 0, DateTimeKind.Utc), address3, "+32474771836");
        User userPending = new User(pendingAuth0Id, "Pending", "Gebruiker", "pending@hogent.be",
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


    [Fact]
    public async Task UserRegistration_Should_Create_User_In_Db()
    {
        // Arrange
        var testUser = new UserDto.RegistrationUser(
            Id: "registerTest",
            FirstName: "Test",
            LastName: "User",
            Email: $"testuser_{Guid.NewGuid()}@example.com", // Ensure unique email
            PhoneNumber: "0499882244",
            Password: "Test@123",
            Address: new AddressDto.GetAdress
            {
                Street = StreetEnum.AFRIKALAAN,
                HouseNumber = "123"
            },
            BirthDate: DateTime.UtcNow.AddYears(-20)
        );

        Factory.mockAuth0UserService.Setup(service => service.RegisterUserAuth0(It.IsAny<UserDto.RegistrationUser>())).ReturnsAsync(testUser);
        // Act

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/User")
        {
            Content = JsonContent.Create(testUser)
        };

        var response = await Client.SendAsync(request);


        // Assert
        response.EnsureSuccessStatusCode();

        // Verify the user exists in the database
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var dbUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == testUser.Email);

        Assert.NotNull(dbUser); // Verify the user exists in the database
        Assert.Equal(testUser.FirstName, dbUser.FirstName);

        // Verify email was sent
        Factory.mockEmailService.Verify(
            service => service.SendEmailAsync(
                It.Is<EmailMessage>(mail => mail.To == "admin@hogent.be")),
            Times.Once);
    }


    [Fact]
    public async Task CreateUser_WithDuplicateEmail_Should_Return_Conflict()
    {
        // Arrange
        var testUser = new UserDto.RegistrationUser(
            Id: "duplicateUser",
            FirstName: "Test",
            LastName: "Duplicate",
            Email: "existinguser@example.com", // Email already exists
            PhoneNumber: "123456789",
            Password: "Test@123",
            Address: new AddressDto.GetAdress
            {
                Street = StreetEnum.AFRIKALAAN,
                HouseNumber = "123",
                Bus = "A"
            },
            BirthDate: DateTime.UtcNow
        );

        Factory.mockAuth0UserService.Setup(service => service.RegisterUserAuth0(It.IsAny<UserDto.RegistrationUser>()))
            .ThrowsAsync(new UserAlreadyExistsException(message: "User already exists"));

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/User")
        {
            Content = JsonContent.Create(testUser)
        };

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WhenExceptionOccurs_Should_Return_InternalServerError()
    {
        // Arrange
        var testUser = new UserDto.RegistrationUser(
            Id: "registerTest",
            FirstName: "Test",
            LastName: "User",
            Email: "testuser@example.com",
            PhoneNumber: "123456789",
            Password: "Test@123",
            Address: new AddressDto.GetAdress
            {
                Street = StreetEnum.AFRIKALAAN,
                HouseNumber = "123",
                Bus = "A"
            },
            BirthDate: DateTime.UtcNow
        );

        Factory.mockAuth0UserService.Setup(service => service.RegisterUserAuth0(It.IsAny<UserDto.RegistrationUser>()))
            .ThrowsAsync(new Exception("Simulated exception"));

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/User")
        {
            Content = JsonContent.Create(testUser)
        };

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }



    [Fact]
    public async Task UserGet_asAdmin_Should_Return_All_Users()
    {
        // Arrange
        var token = GenerateJwtToken("admin", "Admin"); // Generate a valid JWT token for the test
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/User");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<IEnumerable<UserDto.UserBase>>(jsonResponse, JsonOptions);

        Assert.NotNull(users);
        Assert.Equal(4, users.Count());
    }

    [Fact]
    public async Task GetAllUsers_AsNonAdmin_Should_Return_Forbidden()
    {
        // Arrange
        var token = GenerateJwtToken("regularUser", "User"); // Generate JWT token for a regular user
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/User");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAuth0Users_WhenAuth0ServiceUnavailable_Should_Return_ServiceUnavailable()
    {
        // Arrange
        Factory.mockAuth0UserService.Setup(service => service.GetAllUsersAsync())
            .ThrowsAsync(new ErrorApiException("Simulated external service exception", new Exception()));

        var token = GenerateJwtToken("admin", "Admin");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/User/authUsers");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var jsonResponse = await response.Content.ReadAsStringAsync();
        Assert.Contains("Auth0 service is unavailable.", jsonResponse);
    }



    [Fact]
    public async Task UserGetById_AsUser_Should_Return_User()
    {
        // Arrange
        var userId = "testUserId";
        var address4 = new Address("Deckerstraat", "6");
        var testUser = new User(userId, "Test", "User", "testuser@exaample.com", DateTime.UtcNow.AddYears(-20), address4, "+32474771836");
        Role roleUser = new Role(RolesEnum.User);
        testUser.Roles.Add(roleUser);


        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Users.Add(testUser);
        await dbContext.SaveChangesAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/User/{userId}");
        var token = GenerateJwtToken("regularUser", "User", userId); // Generate a valid JWT token for the test
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<UserDto.UserBase>(jsonResponse, JsonOptions);

        Assert.NotNull(user);
        Assert.Equal(testUser.Id, user.Id);
        Assert.Equal(testUser.FirstName, user.FirstName);
        Assert.Equal(testUser.LastName, user.LastName);
        Assert.Equal(testUser.Email, user.Email);
    }

    [Fact]
    public async Task GetUserById_AsAdmin_Should_Return_UserDetails()
    {
        // Arrange
        string userId = "auth0|6713ad614fda04f4b9ae2156";
        var token = GenerateJwtToken("admin", "Admin"); // Generate JWT token for admin
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/User/{userId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<UserDto.UserBase>(jsonResponse, JsonOptions);

        Assert.NotNull(user);
        Assert.Equal(userId, user.Id);
    }


    [Fact]
    public async Task UserGet_asUser_Should_Return_Forbidden()
    {
        // Arrange
        var token = GenerateJwtToken("regularUser", "User"); // Generate a JWT token for a regular user
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/User");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDetails_AsNonAdmin_Should_Return_Forbidden()
    {
        // Arrange
        var userId = "auth0|testUserId";
        var token = GenerateJwtToken("regularUser", "User", "auth0|anotherUserId"); // JWT token for a different user
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/User/{userId}/details");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }


    [Fact]
    public async Task UserUpdate_AsUser_Should_Modify_User_Details()
    {
        // Arrange
        var userId = "testUserId";
        var address4 = new Address("Deckerstraat", "6");
        var testUser = new User(userId, "Test", "User", "testuser@exaample.com", DateTime.UtcNow.AddYears(-20), address4, "+32474771836");
        Role roleUser = new Role(RolesEnum.User);
        testUser.Roles.Add(roleUser);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Users.Add(testUser);
        await dbContext.SaveChangesAsync();

        var updatedUser = new UserDto.UpdateUser
        {
            Id = userId,
            FirstName = "Updated",
            LastName = "User",
            Email = testUser.Email,
            BirthDate = DateTime.UtcNow.AddYears(-20).AddDays(5)
        };

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/User")
        {
            Content = JsonContent.Create(updatedUser)
        };
        var token = GenerateJwtToken("regularUser", "User", userId); // Generate a valid JWT token for the admin
        // var token = GenerateJwtToken();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);


        Factory.mockAuth0UserService.Setup(service => service.UpdateUserAuth0(It.IsAny<UserDto.UpdateUser>())).ReturnsAsync(true);
        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        var dbUser = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        Assert.NotNull(dbUser);
        Assert.Equal(updatedUser.FirstName, dbUser.FirstName);
        Assert.Equal(updatedUser.LastName, dbUser.LastName);
        Assert.Equal(updatedUser.Email, dbUser.Email);
        Assert.Equal(updatedUser.BirthDate, dbUser.BirthDate);

        //Check Admin recieved notifcation
        var admin = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Roles.Any(r => r.Name == RolesEnum.Admin));
        Assert.NotNull(admin);
        var Notification = await dbContext.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.UserId == admin.Id);
        Assert.NotNull(Notification);
        Assert.Equal(Notification.Title_EN, $"User Updated: {testUser.FirstName} {testUser.LastName}");
    }

    [Fact]
    public async Task UserUpdate_AsWrongUser_Should_Return_Forbid()
    {
        // Arrange
        var userId = "testUserId";
        var wrongUserId = "wrongUserId";
        var address4 = new Address("Deckerstraat", "6");
        var testUser = new User(userId, "Test", "User", "testuser@exaample.com", DateTime.UtcNow.AddYears(-20), address4, "+32474771836");
        Role roleUser = new Role(RolesEnum.User);
        testUser.Roles.Add(roleUser);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Users.Add(testUser);
        await dbContext.SaveChangesAsync();

        var updatedUser = new UserDto.UpdateUser
        {
            Id = userId,
            FirstName = "Updated",
            LastName = "User",
            Email = testUser.Email,
            BirthDate = DateTime.UtcNow.AddYears(-20).AddDays(5)
        };

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/User")
        {
            Content = JsonContent.Create(updatedUser)
        };
        var token = GenerateJwtToken("regularUser", "User", wrongUserId); // Generate a valid JWT token for the admin
        // var token = GenerateJwtToken();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);


        Factory.mockAuth0UserService.Setup(service => service.UpdateUserAuth0(It.IsAny<UserDto.UpdateUser>())).ReturnsAsync(true);
        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserUpdateWithUserRole_AsUser_Should_Return_BadRequest()
    {
        // Arrange
        var userId = "testUserId";
        var address4 = new Address("Deckerstraat", "6");
        var testUser = new User(userId, "Test", "User", "testuser@exaample.com", DateTime.UtcNow.AddYears(-20), address4, "+32474771836");
        Role roleUser = new Role(RolesEnum.User);
        testUser.Roles.Add(roleUser);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Users.Add(testUser);
        await dbContext.SaveChangesAsync();

        var updatedUser = new UserDto.UpdateUser
        {
            Id = userId,
            FirstName = "Updated",
            LastName = "User",
            Email = testUser.Email,
            BirthDate = DateTime.UtcNow.AddYears(-20).AddDays(5),
            Roles = [new RoleDto { Name = RolesEnum.BUUTAgent }]
        };

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/User")
        {
            Content = JsonContent.Create(updatedUser)
        };
        var token = GenerateJwtToken("regularUser", "User", userId); // Generate a valid JWT token for the admin
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);


        Factory.mockAuth0UserService.Setup(service => service.UpdateUserAuth0(It.IsAny<UserDto.UpdateUser>())).ReturnsAsync(true);
        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_AsUser_Should_Return_NotFound()
    {
        // Arrange
        var userId = "testUserId";
        var updatedUser = new UserDto.UpdateUser
        {
            Id = userId,
            FirstName = "Updated",
            LastName = "User",
            Email = "testuser@exaample.com",
            BirthDate = DateTime.UtcNow.AddYears(-20).AddDays(5)
        };

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/User")
        {
            Content = JsonContent.Create(updatedUser)
        };
        var token = GenerateJwtToken("regularUser", "User", userId); // Generate a valid JWT token for the admin
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);


        Factory.mockAuth0UserService.Setup(service => service.UpdateUserAuth0(It.IsAny<UserDto.UpdateUser>())).ReturnsAsync(true);
        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_AsAdmin_Should_Modify_User_Details()
    {
        // Arrange
        var userId = "testUserId";
        var address4 = new Address("Deckerstraat", "6");
        var testUser = new User(userId, "Test", "User", "testuser@exaample.com", DateTime.UtcNow.AddYears(-20), address4, "+32474771836");
        Role roleUser = new Role(RolesEnum.User);
        testUser.Roles.Add(roleUser);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Users.Add(testUser);
        await dbContext.SaveChangesAsync();

        var updatedUser = new UserDto.UpdateUser
        {
            Id = userId,
            FirstName = "Updated",
            LastName = "User",
            Email = testUser.Email,
            BirthDate = DateTime.UtcNow.AddYears(-20).AddDays(5)
        };

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/User")
        {
            Content = JsonContent.Create(updatedUser)
        };
        var token = GenerateJwtToken();// Generate a valid JWT token for the admin
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);


        Factory.mockAuth0UserService.Setup(service => service.UpdateUserAuth0(It.IsAny<UserDto.UpdateUser>())).ReturnsAsync(true);
        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        var dbUser = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        Assert.NotNull(dbUser);
        Assert.Equal(updatedUser.FirstName, dbUser.FirstName);
        Assert.Equal(updatedUser.LastName, dbUser.LastName);
        Assert.Equal(updatedUser.Email, dbUser.Email);
        Assert.Equal(updatedUser.BirthDate, dbUser.BirthDate);

        //Check Admin recieved notifcation
        var admin = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Roles.Any(r => r.Name == RolesEnum.Admin));
        Assert.NotNull(admin);
        var Notification = await dbContext.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.UserId == admin.Id);
        Assert.Null(Notification);
    }

    [Fact]
    public async Task UpdateUser_WhenExceptionOccurs_Should_Return_InternalServerError()
    {
        // Arrange
        string userId = "auth0|6713ad614fda04f4b9ae2156";
        var updatedUser = new UserDto.UpdateUser
        {
            Id = userId,
            FirstName = "Updated",
            LastName = "User",
            Email = "testuser@example.com",
            BirthDate = DateTime.UtcNow.AddYears(-20)
        };

        Factory.mockAuth0UserService.Setup(service => service.UpdateUserAuth0(It.IsAny<UserDto.UpdateUser>()))
            .ThrowsAsync(new Exception("Simulated exception"));

        var request = new HttpRequestMessage(HttpMethod.Put, "/api/User")
        {
            Content = JsonContent.Create(updatedUser)
        };

        var token = GenerateJwtToken("regularUser", "User", userId);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }


    [Fact]
    public async Task DeleteUser_AsUser_Should_Remove_User_From_Db()
    {
        // Arrange
        var userId = "testUserId";
        var address4 = new Address("Deckerstraat", "6");
        var testUser = new User(userId, "Test", "User", "testuser@exaample.com", DateTime.UtcNow.AddYears(-20), address4, "+32474771836");
        Role roleUser = new Role(RolesEnum.User);
        testUser.Roles.Add(roleUser);


        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Users.Add(testUser);
        await dbContext.SaveChangesAsync();


        Factory.mockAuth0UserService.Setup(service => service.SoftDeleteAuth0UserAsync(It.IsAny<string>())).ReturnsAsync(true);

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/User/{userId}/softdelete");
        // var token = GenerateJwtToken("admin", "Admin"); // Generate a valid JWT token for the admin
        var token = GenerateJwtToken("regularUser", "User", userId);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        //Check User deleted
        var dbUser = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        Assert.NotNull(dbUser);
        Assert.True(dbUser.IsDeleted);

        //Check Admin recieved notifcation
        var admin = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Roles.Any(r => r.Name == RolesEnum.Admin));
        Assert.NotNull(admin);
        var Notification = await dbContext.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.UserId == admin.Id);
        Assert.NotNull(Notification);
        Assert.Equal(Notification.Title_EN, $"User Deleted : {dbUser.FirstName} {dbUser.LastName}");
    }

    [Fact]
    public async Task DeleteUser_AsAdmin_Should_Remove_User_From_Db()
    {
        // Arrange
        var userId = "testUserId";
        var address4 = new Address("Deckerstraat", "6");
        var testUser = new User(userId, "Test", "User", "testuser@exaample.com", DateTime.UtcNow.AddYears(-20), address4, "+32474771836");
        Role roleUser = new Role(RolesEnum.User);
        testUser.Roles.Add(roleUser);


        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Users.Add(testUser);
        await dbContext.SaveChangesAsync();


        Factory.mockAuth0UserService.Setup(service => service.SoftDeleteAuth0UserAsync(It.IsAny<string>())).ReturnsAsync(true);

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/User/{userId}/softdelete");
        var token = GenerateJwtToken(); // Generate a valid JWT token for the admin

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        //Check User deleted
        var dbUser = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        Assert.NotNull(dbUser);
        Assert.True(dbUser.IsDeleted);

        //Check Admin recieved notifcation
        var admin = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Roles.Any(r => r.Name == RolesEnum.Admin));
        Assert.NotNull(admin);
        var Notification = await dbContext.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.UserId == admin.Id);
        Assert.Null(Notification);
    }

    [Fact]
    public async Task DeleteUser_AsUserWithActiveBooking_Should_Return_BadRequest()
    {
        // Arrange
        var userId = "testUserId";
        var address4 = new Address("Deckerstraat", "6");
        var testUser = new User(userId, "Test", "User", "testuser@exaample.com", DateTime.UtcNow.AddYears(-20), address4, "+32474771836");
        Role roleUser = new Role(RolesEnum.User);
        testUser.Roles.Add(roleUser);

        var booking = new Booking(DateTime.UtcNow, userId, TimeSlot.Afternoon);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Users.Add(testUser);
        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync();

        Factory.mockAuth0UserService.Setup(service => service.SoftDeleteAuth0UserAsync(It.IsAny<string>())).ReturnsAsync(true);

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/User/{userId}/softdelete");
        var token = GenerateJwtToken("regularUser", "User", userId);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }


    [Theory]
    [InlineData(" ")]
    public async Task DeleteUser_AsUser_Should_Return_BadRequest(string? invalidUserId)
    {
        // Arrange
        var userId = "testUserId";

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Factory.mockAuth0UserService.Setup(service => service.SoftDeleteAuth0UserAsync(It.IsAny<string>())).ReturnsAsync(true);

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/User/{invalidUserId}/softdelete");
        // var token = GenerateJwtToken("admin", "Admin"); // Generate a valid JWT token for the admin
        var token = GenerateJwtToken("regularUser", "User", userId);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task DeleteUser_AsUser_Should_Return_MehodNotAllowed(string? invalidUserId)
    {
        // Arrange
        var userId = "testUserId";

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Factory.mockAuth0UserService.Setup(service => service.SoftDeleteAuth0UserAsync(It.IsAny<string>())).ReturnsAsync(true);

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/User/{invalidUserId}/softdelete");

        var token = GenerateJwtToken("regularUser", "User", userId);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_WhenDatabaseExceptionOccurs_Should_Return_InternalServerError()
    {
        // Arrange
        string userId = "auth0|6713ad614fda04f4b9ae2156";
        Factory.mockAuth0UserService.Setup(service => service.SoftDeleteAuth0UserAsync(It.IsAny<string>()))
            .ThrowsAsync(new DatabaseOperationException("Simulated database exception", new Exception()));

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/User/{userId}/softdelete");
        var token = GenerateJwtToken("admin", "Admin");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}