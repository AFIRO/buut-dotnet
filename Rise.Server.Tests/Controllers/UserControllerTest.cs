using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Rise.Server.Controllers;
using Rise.Shared.Users;
using Rise.Services.Users;
using Rise.Shared.Bookings;
using Rise.Shared.Services;
using Auth0.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualBasic.CompilerServices;
using Rise.Domain.Users;
using Rise.Shared.Enums;
using Rise.Services.Events;
using Rise.Services.Events.User;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Rise.Server.Tests.Controllers;

public class UserControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<IAuth0UserService> _auth0UserServiceMock;
    private readonly Mock<IValidationService> _validationServiceMock;

    private readonly Mock<IEventDispatcher> _eventDispatcherMock;

    private readonly UserController _userController;
    private readonly Mock<ILogger<UserController>> _loggerMock;

    public UserControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _auth0UserServiceMock = new Mock<IAuth0UserService>();
        _validationServiceMock = new Mock<IValidationService>();
        _eventDispatcherMock = new Mock<IEventDispatcher>();
        _loggerMock = new Mock<ILogger<UserController>>();
        _userController = new UserController(_userServiceMock.Object, _auth0UserServiceMock.Object,
            _validationServiceMock.Object, _eventDispatcherMock.Object, _loggerMock.Object);
    }

    private UserDto.RegistrationUser CreateRegistrationUser(int id)
    {
        return CreateRegistrationUser(id.ToString());
    }

    private UserDto.RegistrationUser CreateRegistrationUser(string userid = "1")
    {
        return new UserDto.RegistrationUser($"John{userid}", $"Doe{userid}", $"john{userid}.doe@example.com", "+3245784578",
            $"verystrongpassword{userid}", userid,
            new AddressDto.GetAdress() { Street = StreetEnum.AFRIKALAAN, HouseNumber = $"1{userid}" },
            new DateTime(1990, 1, 1));
    }

    private UserDto.UserDetails CreateUserDetails(int userid)
    {
        return CreateUserDetails(userid.ToString());
    }

    private UserDto.UserDetails CreateUserDetails(string userid = "1")
    {
        return new UserDto.UserDetails()
        {
            Id = userid,
            FirstName = $"Keoma{userid}",
            LastName = $"King{userid}",
            Email = $"kingkeoma{userid}@gmail.in",
            Address = new AddressDto.GetAdress() { Street = StreetEnum.AFRIKALAAN, HouseNumber = $"1{userid}" },
            Roles = [new RoleDto() { Name = RolesEnum.User }],
            BirthDate = new DateTime(1990, 1, IntegerType.FromString(userid))
        };
    }

    private UserDto.UserBase CreateUserBase(int id)
    {
        return CreateUserBase(id.ToString());
    }

    private UserDto.UserBase CreateUserBase(string id = "1")
    {
        return new UserDto.UserBase(id, $"Keoma{id}", $"King{id}", $"kingkeoma{id}@gmail.in",
            [new RoleDto() { Name = RolesEnum.User }]);
    }

    private UserDto.UpdateUser CreateUpdateUser(int id)
    {
        return CreateUpdateUser(id.ToString());
    }

    private UserDto.UpdateUser CreateUpdateUser(string id = "1")
    {
        return new UserDto.UpdateUser
        {
            Id = id,
            FirstName = $"John{id}",
            LastName = $"Doe{id}",
            Email = $"john{id}.doe@example.com"
        };
    }

    [Fact]
    public async Task GetAllUsers_ShouldReturnOkResult_WhenUsersExist()
    {
        // Arrange
        var users = new List<UserDto.UserBase>();
        for (int i = 0; i < 10; i++)
        {
            users.Add(CreateUserBase(i));
        }

        _userServiceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(users);

        // Act
        var result = await _userController.GetAllUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(users, okResult.Value);
    }

    [Fact]
    public async Task GetAllUsers_ShouldReturnNotFound_WhenNoUsersExist()
    {
        // Arrange
        _userServiceMock.Setup(s => s.GetAllAsync()).ReturnsAsync((IEnumerable<UserDto.UserBase>)null);

        // Act
        var result = await _userController.GetAllUsers();

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Get_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var userId = "1";
        var user = CreateUserBase(userId);
        _userServiceMock.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(user);

        // Mock authenticated user context
        _userController.ControllerContext = CreateMockControllerContext(userId, RolesEnum.User);

        // Act
        var result = await _userController.Get(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result); // Verify the response is an OkObjectResult
        Assert.Equal(200, okResult.StatusCode); // Verify the status code is 200 (OK)

        var returnedUser = Assert.IsType<UserDto.UserBase>(okResult.Value); // Extract and verify the value
        Assert.Equal(user.Id, returnedUser.Id);
        Assert.Equal(user.FirstName, returnedUser.FirstName);
        Assert.Equal(user.LastName, returnedUser.LastName);
        Assert.Equal(user.Email, returnedUser.Email);
        Assert.Equal(user.Roles, returnedUser.Roles);
    }


    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = "1";
        _userServiceMock.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync((UserDto.UserBase)null);

        // Mock authenticated user context
        _userController.ControllerContext = CreateMockControllerContext(userId, RolesEnum.User);

        // Act
        var result = await _userController.Get(userId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result); // Verify the response is a NotFoundObjectResult
        Assert.Equal(404, notFoundResult.StatusCode); // Verify the status code is 404 (Not Found)

        // Verify the content of the NotFound response
        var responseObject = notFoundResult.Value;
        Assert.NotNull(responseObject);
        var message = responseObject.GetType().GetProperty("message")?.GetValue(responseObject, null)?.ToString();
        Assert.Equal($"User with ID {userId} was not found.", message);
    }

    [Fact]
    public async Task Get_ShouldHandleException_WhenUnexpectedErrorOccurs()
    {
        // Arrange
        var userId = "1";
        _userServiceMock.Setup(s => s.GetUserByIdAsync(userId)).ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _userController.Get(userId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result); // Verify it's an ObjectResult
        Assert.Equal(500, objectResult.StatusCode); // Verify the status code is 500 (Internal Server Error)

        // Verify the content of the 500 response
        var responseObject = objectResult.Value;
        Assert.NotNull(responseObject);

        var message = responseObject.GetType().GetProperty("message")?.GetValue(responseObject, null)?.ToString();

        Assert.Equal("An unexpected error occurred while fetching the user.", message);

    }


    [Fact]
    public async Task GetDetails_ShouldReturnUserDetails_WhenUserExists()
    {
        // Arrange
        var userId = "1";
        var userDetails = CreateUserDetails(userId);

        _userServiceMock.Setup(s => s.GetUserDetailsByIdAsync(userId)).ReturnsAsync(userDetails);

        // Mock authenticated user context
        _userController.ControllerContext = CreateMockControllerContext(userId, RolesEnum.User);

        // Act
        var result = await _userController.GetDetails(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result); // Verify the response is an OK (200) status
        Assert.Equal(200, okResult.StatusCode);

        var returnedUser = Assert.IsType<UserDto.UserDetails>(okResult.Value); // Extract the returned user details
        Assert.Equal(userDetails.Id, returnedUser.Id);
        Assert.Equal(userDetails.FirstName, returnedUser.FirstName);
        Assert.Equal(userDetails.LastName, returnedUser.LastName);
        Assert.Equal(userDetails.Email, returnedUser.Email);
    }

    [Fact]
    public async Task GetDetails_ShouldReturnNotFound_WhenUserDetailsNotExist()
    {
        // Arrange
        var userId = "1";
        _userServiceMock.Setup(s => s.GetUserDetailsByIdAsync(userId)).ReturnsAsync((UserDto.UserDetails)null);

        // Mock authenticated user context
        _userController.ControllerContext = CreateMockControllerContext(userId, RolesEnum.User);

        // Act
        var result = await _userController.GetDetails(userId);

        // Assert
        var notFoundObjectResult = Assert.IsType<NotFoundObjectResult>(result); // Validate the response is an ObjectResult
        Assert.Equal(404, notFoundObjectResult.StatusCode); // Ensure the status code is 404

        // Check the response content
        var responseObject = notFoundObjectResult.Value;
        Assert.NotNull(responseObject);

        // Validate the message property in the response
        var message = responseObject.GetType().GetProperty("message")?.GetValue(responseObject, null)?.ToString();
        Assert.Equal($"User with ID {userId} was not found.", message);
    }


    [Fact]
    public async Task Post_ShouldReturnOk_WhenUserCreatedSuccessfully()
    {
        // Arrange
        var userDetails = CreateRegistrationUser();
        _auth0UserServiceMock.Setup(a => a.RegisterUserAuth0(userDetails)).ReturnsAsync(userDetails);
        _userServiceMock.Setup(s => s.CreateUserAsync(userDetails)).ReturnsAsync((true, "UserCreatedSuccess"));

        // Act
        var result = await _userController.Post(userDetails);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task Post_ShouldReturnConflict_WhenUserAlreadyExists()
    {
        // Arrange
        var userDetails = CreateRegistrationUser();
        _auth0UserServiceMock.Setup(a => a.RegisterUserAuth0(userDetails))
            .ThrowsAsync(new UserAlreadyExistsException("User already exists"));

        // Act
        var result = await _userController.Post(userDetails);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflictResult.StatusCode);
    }

    [Fact]
    public async Task Post_ShouldReturnBadRequest_WhenUserDetailsAreInvalid()
    {
        // Arrange
        UserDto.RegistrationUser userDetails = null;

        // Act
        var result = await _userController.Post(userDetails);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task Post_ShouldReturnInternalServerError_WhenUnexpectedExceptionOccurs()
    {
        // Arrange
        var userDetails = CreateRegistrationUser();
        _auth0UserServiceMock.Setup(a => a.RegisterUserAuth0(userDetails))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _userController.Post(userDetails);

        // Assert
        var internalServerErrorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, internalServerErrorResult.StatusCode);
    }

    [Fact]
    public async Task Put_ShouldReturnOk_WhenUserUpdatedSuccessfully()
    {
        // Arrange
        var userDetails = CreateUpdateUser();
        var existingUser = new UserDto.UserBase(
            userDetails.Id,
            "ExistingFirstName",
            "ExistingLastName",
            "existing.email@example.com",
            [new RoleDto { Name = RolesEnum.User }]);

        // Mock the existing user in the database
        _userServiceMock.Setup(s => s.GetUserByIdAsync(userDetails.Id)).ReturnsAsync(existingUser);

        // Mock successful updates
        _userServiceMock.Setup(s => s.UpdateUserAsync(It.IsAny<UserDto.UpdateUser>())).ReturnsAsync(true);
        _auth0UserServiceMock.Setup(a => a.UpdateUserAuth0(It.IsAny<UserDto.UpdateUser>())).ReturnsAsync(true);

        // Mock authenticated user context
        _userController.ControllerContext = CreateMockControllerContext(userDetails.Id, RolesEnum.User);

        // Act
        var result = await _userController.Put(userDetails);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result); // Verify response type
        Assert.Equal(200, okResult.StatusCode); // Verify the status code
    }



    [Fact]
    public async Task Put_ShouldReturnBadRequest_WhenUserDetailsAreInvalid()
    {
        // Arrange
        UserDto.UpdateUser userDetails = null;

        // Act
        var result = await _userController.Put(userDetails);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }


    [Fact]
    public async Task Put_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userDetails = CreateUpdateUser();

        // Simulate user not found by returning null
        _userServiceMock.Setup(s => s.GetUserByIdAsync(userDetails.Id)).ReturnsAsync((UserDto.UserBase)null);

        // Mock authenticated user context
        _userController.ControllerContext = CreateMockControllerContext(userDetails.Id, RolesEnum.User);

        // Act
        var result = await _userController.Put(userDetails);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result); // Expect NotFoundObjectResult
        Assert.Equal(404, notFoundResult.StatusCode); // Verify status code is 404

        // Verify the message in the response
        var responseObject = notFoundResult.Value;
        Assert.NotNull(responseObject); // Ensure response content is not null

        // Use reflection to access the message property dynamically
        var message = responseObject.GetType().GetProperty("message")?.GetValue(responseObject, null)?.ToString();
        Assert.Equal("User not found.", message);
    }


    [Fact]
    public async Task Delete_ShouldReturnOk_WhenUserDeletedSuccessfully()
    {
        // Arrange
        var userId = "1";

        var user = new UserDto.UserBase(
            userId,
            "John",
            "Doe",
            "john.doe@example.com",
            [new RoleDto { Name = RolesEnum.User }]
        );

        // Mock authenticated user context
        _userController.ControllerContext = CreateMockControllerContext(user.Id, RolesEnum.User);

        _userServiceMock.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(user);
        _userServiceMock.Setup(s => s.SoftDeleteUserAsync(userId)).ReturnsAsync(true);
        _auth0UserServiceMock.Setup(a => a.SoftDeleteAuth0UserAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _userController.Delete(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.NotNull(okResult.Value);
    }


    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = "1";
        _userServiceMock
            .Setup(s => s.SoftDeleteUserAsync(userId))
            .ThrowsAsync(new UserNotFoundException($"User with ID {userId} not found."));

        // Mock authenticated user context
        _userController.ControllerContext = CreateMockControllerContext(userId, RolesEnum.User);

        // Act
        var result = await _userController.Delete(userId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task Delete_ShouldReturnBadRequest_WhenUserHasActiveBookings()
    {
        // Arrange
        var userId = "1";
        _validationServiceMock.Setup(b => b.CheckActiveBookings(userId)).ReturnsAsync(true);

        // Mock authenticated user context
        _userController.ControllerContext = CreateMockControllerContext(userId, RolesEnum.User);

        // Act
        var result = await _userController.Delete(userId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult);
        Assert.Equal(400, badRequestResult.StatusCode);
        Assert.NotNull(badRequestResult.Value);
    }


    [Fact]
    public async Task IsEmailTaken_ShouldReturnTrue_WhenEmailIsTaken()
    {
        // Arrange
        string email = "taken@example.com";
        _auth0UserServiceMock.Setup(a => a.IsEmailTakenAsync(email)).ReturnsAsync(true);

        // Act
        var result = await _userController.IsEmailTaken(email);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.True((bool)okResult.Value);
    }

    [Fact]
    public async Task IsEmailTaken_ShouldReturnFalse_WhenEmailIsNotTaken()
    {
        // Arrange
        string email = "not_taken@example.com";
        _auth0UserServiceMock.Setup(a => a.IsEmailTakenAsync(email)).ReturnsAsync(false);

        // Act
        var result = await _userController.IsEmailTaken(email);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.False((bool)okResult.Value);
    }

    [Fact]
    public async Task GetFilteredUsers_ShouldReturnOk_WhenUsersAreFound()
    {
        // Arrange
        var filter = new UserFilter();
        var users = new List<UserDto.UserBase>();

        for (int i = 0; i < 10; i++)
        {
            users.Add(CreateUserBase(i));
        }

        _userServiceMock.Setup(s => s.GetFilteredUsersAsync(filter)).ReturnsAsync(users);

        // Act
        var result = await _userController.GetFilteredUsers(filter);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(users, okResult.Value);
    }

    [Fact]
    public async Task GetFilteredUsers_ShouldReturnNotFound_WhenNoUsersAreFound()
    {
        // Arrange
        var filter = new UserFilter();
        _userServiceMock.Setup(s => s.GetFilteredUsersAsync(filter))
            .ReturnsAsync(new List<UserDto.UserBase>());

        // Act
        var result = await _userController.GetFilteredUsers(filter);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
        Assert.Equal("No users found matching the given filters.", notFoundResult.Value);
    }

    [Fact]
    public async Task GetFilteredUsers_ShouldReturnBadRequest_WhenFilterIsInvalid()
    {
        // Arrange
        var filter = new UserFilter();
        _userServiceMock.Setup(s => s.GetFilteredUsersAsync(filter))
            .ThrowsAsync(new ArgumentException("Invalid filter"));

        // Act
        var result = await _userController.GetFilteredUsers(filter);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
        Assert.Equal("Invalid filter argument: Invalid filter", badRequestResult.Value);
    }

    [Fact]
    public async Task GetFilteredUsers_ShouldReturnForbid_WhenUnauthorizedAccessExceptionIsThrown()
    {
        // Arrange
        var filter = new UserFilter();
        _userServiceMock.Setup(s => s.GetFilteredUsersAsync(filter))
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

        // Act
        var result = await _userController.GetFilteredUsers(filter);

        // Assert
        var forbidResult = Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetFilteredUsers_ShouldReturnInternalServerError_WhenUnexpectedErrorOccurs()
    {
        // Arrange
        var filter = new UserFilter();
        _userServiceMock.Setup(s => s.GetFilteredUsersAsync(filter)).ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _userController.GetFilteredUsers(filter);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("An unexpected error occurred: Unexpected error", objectResult.Value);
    }


    [Fact]
    public async Task Post_DispatchesUserRegisteredEvent_WhenUserCreatedSuccessfully()
    {
        // Arrange
        var userDetails = CreateRegistrationUser();
        var userBase = new UserDto.UserBase(
            id: "1",
            firstName: "John",
            lastName: "Doe",
            email: "john.doe@example.com",
            roles: [new RoleDto { Name = RolesEnum.User }]);

        _auth0UserServiceMock.Setup(a => a.RegisterUserAuth0(userDetails)).ReturnsAsync(userDetails);
        _userServiceMock.Setup(s => s.CreateUserAsync(userDetails)).ReturnsAsync((true, "UserCreatedSuccess"));

        // Act
        await _userController.Post(userDetails);

        // Assert
        _eventDispatcherMock.Verify(dispatcher => dispatcher.DispatchAsync(
            It.Is<UserRegisteredEvent>(e =>
                e.UserId == userDetails.Id &&
                e.FirstName == userDetails.FirstName &&
                e.LastName == userDetails.LastName)),
            Times.Once);

        _eventDispatcherMock.VerifyNoOtherCalls(); // Ensure no other events were dispatched
    }

    [Fact]
    public async Task Delete_DispatchesUserDeletedEvent_WhenUserDeletedSuccessfully()
    {
        // Arrange
        var userId = "1";
        var user = new UserDto.UserBase(
            id: userId,
            firstName: "John",
            lastName: "Doe",
            email: "john.doe@example.com",
            roles: [new RoleDto { Name = RolesEnum.User }]);

        _userServiceMock.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(user);
        _userServiceMock.Setup(s => s.SoftDeleteUserAsync(userId)).ReturnsAsync(true);
        _auth0UserServiceMock.Setup(a => a.SoftDeleteAuth0UserAsync(userId)).ReturnsAsync(true);

        // Mock authenticated user context
        _userController.ControllerContext = CreateMockControllerContext(user.Id, RolesEnum.User);

        // Act
        await _userController.Delete(userId);

        // Assert
        _eventDispatcherMock.Verify(dispatcher => dispatcher.DispatchAsync(
            It.Is<UserDeletedEvent>(e =>
                e.UserId == user.Id &&
                e.FirstName == user.FirstName &&
                e.LastName == user.LastName)),
            Times.Once);

        _eventDispatcherMock.VerifyNoOtherCalls(); // Ensure no other events were dispatched
    }

    [Fact]
    public async Task Post_DoesNotDispatchEvent_WhenUserCreationFails()
    {
        // Arrange
        var userDetails = CreateRegistrationUser();
        _auth0UserServiceMock.Setup(a => a.RegisterUserAuth0(userDetails)).ReturnsAsync(userDetails);
        _userServiceMock.Setup(s => s.CreateUserAsync(userDetails)).ReturnsAsync((false, "UserCreationFailed"));

        // Act
        await _userController.Post(userDetails);

        // Assert
        _eventDispatcherMock.Verify(dispatcher => dispatcher.DispatchAsync(It.IsAny<UserRegisteredEvent>()), Times.Never);
    }

    [Fact]
    public async Task Delete_DoesNotDispatchEvent_WhenUserDeletionFails()
    {
        // Arrange
        var userId = "1";
        _userServiceMock.Setup(s => s.SoftDeleteUserAsync(userId)).ReturnsAsync(false);

        // Act
        await _userController.Delete(userId);

        // Assert
        _eventDispatcherMock.Verify(dispatcher => dispatcher.DispatchAsync(It.IsAny<UserDeletedEvent>()), Times.Never);
    }


    [Fact]
    public async Task Put_DispatchesUserUpdatedEvent_WhenUserUpdatesOwnProfile()
    {
        // Arrange
        var userDetails = CreateUpdateUser();
        var authenticatedUserId = userDetails.Id;

        var userBase = CreateUserBase(userDetails.Id);
        _userServiceMock.Setup(s => s.GetUserByIdAsync(userDetails.Id)).ReturnsAsync(userBase);
        _auth0UserServiceMock.Setup(s => s.UpdateUserAuth0(It.IsAny<UserDto.UpdateUser>())).ReturnsAsync(true);
        _userServiceMock.Setup(s => s.UpdateUserAsync(It.IsAny<UserDto.UpdateUser>())).ReturnsAsync(true);

        // Mock user identity
        _userController.ControllerContext = CreateMockControllerContext(authenticatedUserId, RolesEnum.User);

        // Act
        var result = await _userController.Put(userDetails);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        _eventDispatcherMock.Verify(dispatcher => dispatcher.DispatchAsync(
            It.Is<UserUpdatedEvent>(e =>
                e.UserId == userDetails.Id &&
                e.FirstName == userBase.FirstName &&
                e.LastName == userBase.LastName)),
            Times.Once);
    }

    [Fact]
    public async Task Put_DispatchesUserValidationEvent_WhenAdminUpdatesPendingUserRole()
    {
        // Arrange
        var userDetails = CreateUpdateUser();
        userDetails = userDetails with { Roles = [new RoleDto { Name = RolesEnum.User }] }; // Change from Pending to User

        var userBase = CreateUserBase(userDetails.Id);
        userBase = userBase with { Roles = [new RoleDto { Name = RolesEnum.Pending }] }; // Original role is Pending

        _userServiceMock.Setup(s => s.GetUserByIdAsync(userDetails.Id)).ReturnsAsync(userBase);
        _auth0UserServiceMock.Setup(s => s.UpdateUserAuth0(It.IsAny<UserDto.UpdateUser>())).ReturnsAsync(true);
        _userServiceMock.Setup(s => s.UpdateUserAsync(It.IsAny<UserDto.UpdateUser>())).ReturnsAsync(true);
        _auth0UserServiceMock.Setup(a => a.AssignRoleToUser(userDetails)).ReturnsAsync(true);
        _userServiceMock.Setup(s => s.UpdateUserRolesAsync(userDetails.Id, userDetails.Roles)).ReturnsAsync(true);

        // Mock admin identity
        _userController.ControllerContext = CreateMockControllerContext("adminId", RolesEnum.Admin);

        // Act
        var result = await _userController.Put(userDetails);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        _eventDispatcherMock.Verify(dispatcher => dispatcher.DispatchAsync(
            It.Is<UserValidationEvent>(e =>
                e.UserId == userDetails.Id &&
                e.FirstName == userBase.FirstName &&
                e.LastName == userBase.LastName)),
            Times.Once);
    }


    [Fact]
    public async Task Put_DispatchesUserRoleUpdatedEvent_WhenAdminUpdatesUserRoles()
    {
        // Arrange
        var userDetails = CreateUpdateUser();
        userDetails = userDetails with
        {
            Roles = [new RoleDto { Name = RolesEnum.User }, new RoleDto { Name = RolesEnum.BUUTAgent }]
        };

        var userBase = CreateUserBase(userDetails.Id);
        userBase = userBase with
        {
            Roles = [new RoleDto { Name = RolesEnum.User }] // Existing role is User
        };

        _userServiceMock.Setup(s => s.GetUserByIdAsync(userDetails.Id)).ReturnsAsync(userBase);
        _auth0UserServiceMock.Setup(s => s.UpdateUserAuth0(It.IsAny<UserDto.UpdateUser>())).ReturnsAsync(true);
        _userServiceMock.Setup(s => s.UpdateUserAsync(It.IsAny<UserDto.UpdateUser>())).ReturnsAsync(true);
        _auth0UserServiceMock.Setup(a => a.AssignRoleToUser(userDetails)).ReturnsAsync(true);
        _userServiceMock.Setup(s => s.UpdateUserRolesAsync(userDetails.Id, userDetails.Roles)).ReturnsAsync(true);

        // Mock admin identity
        _userController.ControllerContext = CreateMockControllerContext("adminId", RolesEnum.Admin);

        // Act
        var result = await _userController.Put(userDetails);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        _eventDispatcherMock.Verify(dispatcher => dispatcher.DispatchAsync(
            It.Is<UserRoleUpdatedEvent>(e =>
                e.UserId == userDetails.Id &&
                e.OldRoles.Contains(RolesEnum.User) &&
                e.NewRoles.Contains(RolesEnum.User) &&
                e.NewRoles.Contains(RolesEnum.BUUTAgent))),
            Times.Once);

        _eventDispatcherMock.VerifyNoOtherCalls(); // Ensure no other events are dispatched
    }



    [Fact]
    public async Task Put_Returns500_WhenRoleUpdateFailsInDatabase()
    {
        // Arrange
        var userDetails = CreateUpdateUser();
        userDetails = userDetails with
        {
            Roles = [new RoleDto { Name = RolesEnum.User }, new RoleDto { Name = RolesEnum.BUUTAgent }] // Change from Pending to User
        };

        var userBase = CreateUserBase(userDetails.Id);
        userBase = userBase with
        {
            Roles = [new RoleDto { Name = RolesEnum.Pending }] // Original role is Pending
        };

        // Properly mock service methods
        _userServiceMock.Setup(s => s.GetUserByIdAsync(userDetails.Id)).ReturnsAsync(userBase); // User exists
        _auth0UserServiceMock.Setup(a => a.UpdateUserAuth0(It.IsAny<UserDto.UpdateUser>())).ReturnsAsync(true); // User updated in Auth0
        _auth0UserServiceMock.Setup(a => a.AssignRoleToUser(userDetails)).ReturnsAsync(true); // Roles assigned in Auth0
        _userServiceMock.Setup(s => s.UpdateUserAsync(It.IsAny<UserDto.UpdateUser>())).ReturnsAsync(true); // User updated in the database
        _userServiceMock.Setup(s => s.UpdateUserRolesAsync(userDetails.Id, userDetails.Roles)).ReturnsAsync(false); // Role update fails in the database

        // Mock admin identity
        _userController.ControllerContext = CreateMockControllerContext("adminId", RolesEnum.Admin);

        // Act
        var result = await _userController.Put(userDetails);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result); // Expect ObjectResult (500 Internal Server Error)
        Assert.Equal(500, objectResult.StatusCode); // Confirm status code is 500

        // Dynamically validate the structure of the response object
        var responseObject = objectResult.Value;
        Assert.NotNull(responseObject);

        // Validate the message content
        var messageProperty = responseObject.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        var messageValue = messageProperty.GetValue(responseObject)?.ToString();
        Assert.Equal("Failed to update user roles in the local database.", messageValue);

        // Verify the UpdateUserAsync was called
        _userServiceMock.Verify(s => s.UpdateUserAsync(It.IsAny<UserDto.UpdateUser>()), Times.Once);

        // Verify the AssignRoleToUser was called
        _auth0UserServiceMock.Verify(a => a.AssignRoleToUser(userDetails), Times.Once);

        // Verify the UpdateUserRolesAsync was called
        _userServiceMock.Verify(s => s.UpdateUserRolesAsync(userDetails.Id, userDetails.Roles), Times.Once);
    }


    [Fact]
    public async Task Put_ReturnsBadRequest_WhenNonAdminUpdatesRoles()
    {
        // Arrange
        var userDetails = CreateUpdateUser();
        userDetails = userDetails with { Roles = [new RoleDto { Name = RolesEnum.User }] }; // Change from Pending to User

        _userServiceMock.Setup(s => s.GetUserByIdAsync(userDetails.Id)).ReturnsAsync(CreateUserBase(userDetails.Id));

        // Mock non-admin identity
        _userController.ControllerContext = CreateMockControllerContext(userDetails.Id, RolesEnum.User);

        // Act
        var result = await _userController.Put(userDetails);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);

        // Dynamically validate the structure of the response object
        var responseObject = badRequestResult.Value;
        Assert.NotNull(responseObject);

        // Validate the 'message' property within the response
        var messageProperty = responseObject.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        var messageValue = messageProperty.GetValue(responseObject)?.ToString();
        Assert.Equal("You are not authorized to update roles.", messageValue);
    }

    [Fact]
    public async Task Put_ReturnsForbid_WhenUserTriesToUpdateAnotherUsersDetails()
    {
        // Arrange
        var userDetails = CreateUpdateUser();
        var anotherUserId = "anotherUserId";

        _userServiceMock.Setup(s => s.GetUserByIdAsync(userDetails.Id)).ReturnsAsync(CreateUserBase(userDetails.Id));

        // Mock non-admin identity
        _userController.ControllerContext = CreateMockControllerContext(anotherUserId, RolesEnum.User);

        // Act
        var result = await _userController.Put(userDetails);

        // Assert
        var forbidResult = Assert.IsType<ForbidResult>(result);
    }



    private ControllerContext CreateMockControllerContext(string userId, RolesEnum role)
    {
        var mockIdentity = new ClaimsIdentity(new[]
        {
        new Claim(ClaimTypes.NameIdentifier, userId),
        new Claim(ClaimTypes.Role, role.ToString())
    });

        var mockPrincipal = new ClaimsPrincipal(mockIdentity);

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = mockPrincipal
            }
        };
    }
}

