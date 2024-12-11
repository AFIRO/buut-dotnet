// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Auth0.ManagementApi;
// using Auth0.ManagementApi.Models;
// using Auth0.ManagementApi.Paging;
// using Auth0.Core.Exceptions;
// using Microsoft.Extensions.Logging;
// using Moq;
// using Rise.Services.Users;
// using Rise.Shared.Users;
// using Rise.Shared.Enums;
// using Xunit;

// namespace Rise.Services.Tests.Users
// {
//     public class Auth0UserServiceTests
//     {
//         private readonly Mock<IManagementApiClient> _mockManagementApiClient;
//         private readonly Mock<ILogger<Auth0UserService>> _mockLogger;
//         private readonly Auth0UserService _auth0UserService;

//         public Auth0UserServiceTests()
//         {
//             _mockManagementApiClient = new Mock<IManagementApiClient>();
//             _mockLogger = new Mock<ILogger<Auth0UserService>>();
//             _auth0UserService = new Auth0UserService(_mockManagementApiClient.Object, _mockLogger.Object);
//         }

//         [Fact]
//         public async Task GetAllUsersAsync_ShouldReturnUsers_WhenUsersExist()
//         {
//             // Arrange
//             var users = new List<User>
//             {
//                 new User { Email = "user1@example.com", FirstName = "John", LastName = "Doe", Blocked = false },
//                 new User { Email = "user2@example.com", FirstName = "Jane", LastName = "Smith", Blocked = true }
//             };
//             _mockManagementApiClient
//                 .Setup(client => client.Users.GetAllAsync(It.IsAny<GetUsersRequest>(), It.IsAny<PaginationInfo>(), It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(users);

//             // Act
//             var result = await _auth0UserService.GetAllUsersAsync();

//             // Assert
//             Assert.NotNull(result);
//             Assert.Equal(2, result.Count());
//             Assert.Contains(result, user => user.Email == "user1@example.com");
//             Assert.Contains(result, user => user.Email == "user2@example.com");
//         }

//         [Fact]
//         public async Task GetAllUsersAsync_ShouldThrowExternalServiceException_WhenApiExceptionOccurs()
//         {
//             // Arrange
//             _mockManagementApiClient
//                 .Setup(client => client.Users.GetAllAsync(It.IsAny<GetUsersRequest>(), It.IsAny<PaginationInfo>()))
//                 .ThrowsAsync(new ApiException("Auth0 API error"));

//             // Act & Assert
//             await Assert.ThrowsAsync<ExternalServiceException>(() => _auth0UserService.GetAllUsersAsync());
//         }

//         [Fact]
//         public async Task GetUserByIdAsync_ShouldReturnUser_WhenUserExists()
//         {
//             // Arrange
//             var userId = "auth0|12345";
//             var user = new User { Email = "user@example.com", FirstName = "John", LastName = "Doe", Blocked = false };
//             _mockManagementApiClient
//                 .Setup(client => client.Users.GetAsync(userId))
//                 .ReturnsAsync(user);

//             // Act
//             var result = await _auth0UserService.GetUserByIdAsync(userId);

//             // Assert
//             Assert.NotNull(result);
//             Assert.Equal(user.Email, result.Email);
//             Assert.Equal(user.FirstName, result.FirstName);
//             Assert.Equal(user.LastName, result.LastName);
//             Assert.False(result.Blocked);
//         }

//         [Fact]
//         public async Task GetUserByIdAsync_ShouldThrowExternalServiceException_WhenApiExceptionOccurs()
//         {
//             // Arrange
//             var userId = "auth0|12345";
//             _mockManagementApiClient
//                 .Setup(client => client.Users.GetAsync(userId))
//                 .ThrowsAsync(new ApiException("Auth0 API error"));

//             // Act & Assert
//             await Assert.ThrowsAsync<ExternalServiceException>(() => _auth0UserService.GetUserByIdAsync(userId));
//         }

//         [Fact]
//         public async Task RegisterUserAuth0_ShouldReturnRegisteredUser_WhenSuccessful()
//         {
//             // Arrange
//             var userDto = new UserDto.RegistrationUser("John", "Doe", "user@example.com", null, "password", null, null, null);
//             var createdUser = new User { UserId = "auth0|12345", Email = userDto.Email, FirstName = userDto.FirstName, LastName = userDto.LastName };

//             _mockManagementApiClient
//                 .Setup(client => client.Users.CreateAsync(It.IsAny<UserCreateRequest>()))
//                 .ReturnsAsync(createdUser);

//             _mockManagementApiClient
//                 .Setup(client => client.Roles.GetAllAsync(It.IsAny<GetRolesRequest>()))
//                 .ReturnsAsync(new List<Role> { new Role { Name = RolesEnum.Pending.ToString(), Id = "role_pending" } });

//             // Act
//             var result = await _auth0UserService.RegisterUserAuth0(userDto);

//             // Assert
//             Assert.NotNull(result);
//             Assert.Equal(userDto.Email, result.Email);
//             Assert.Equal(userDto.FirstName, result.FirstName);
//             Assert.Equal(userDto.LastName, result.LastName);
//         }

//         [Fact]
//         public async Task RegisterUserAuth0_ShouldThrowUserAlreadyExistsException_WhenUserAlreadyExists()
//         {
//             // Arrange
//             var userDto = new UserDto.RegistrationUser("John", "Doe", "user@example.com", null, "password", null, null, null);
//             _mockManagementApiClient
//                 .Setup(client => client.Users.GetAllAsync(It.Is<GetUsersRequest>(r => r.Query.Contains(userDto.Email))))
//                 .ReturnsAsync(new List<User> { new User { Email = userDto.Email } });

//             // Act & Assert
//             await Assert.ThrowsAsync<UserAlreadyExistsException>(() => _auth0UserService.RegisterUserAuth0(userDto));
//         }

//         [Fact]
//         public async Task UpdateUserAuth0_ShouldReturnTrue_WhenUpdateIsSuccessful()
//         {
//             // Arrange
//             var userDto = new UserDto.UpdateUser { Id = "auth0|12345", Email = "updated@example.com", FirstName = "UpdatedName" };
//             var updatedUser = new User { UserId = userDto.Id, Email = userDto.Email, FirstName = userDto.FirstName };

//             _mockManagementApiClient
//                 .Setup(client => client.Users.UpdateAsync(userDto.Id, It.IsAny<UserUpdateRequest>()))
//                 .ReturnsAsync(updatedUser);

//             // Act
//             var result = await _auth0UserService.UpdateUserAuth0(userDto);

//             // Assert
//             Assert.True(result);
//         }

//         [Fact]
//         public async Task UpdateUserAuth0_ShouldThrowExternalServiceException_WhenApiExceptionOccurs()
//         {
//             // Arrange
//             var userDto = new UserDto.UpdateUser { Id = "auth0|12345", Email = "updated@example.com", FirstName = "UpdatedName" };
//             _mockManagementApiClient
//                 .Setup(client => client.Users.UpdateAsync(userDto.Id, It.IsAny<UserUpdateRequest>()))
//                 .ThrowsAsync(new ApiException("Auth0 API error"));

//             // Act & Assert
//             await Assert.ThrowsAsync<ExternalServiceException>(() => _auth0UserService.UpdateUserAuth0(userDto));
//         }

//         [Fact]
//         public async Task IsEmailTakenAsync_ShouldReturnTrue_WhenEmailExists()
//         {
//             // Arrange
//             var email = "existing@example.com";
//             _mockManagementApiClient
//                 .Setup(client => client.Users.GetAllAsync(It.Is<GetUsersRequest>(r => r.Query.Contains(email))))
//                 .ReturnsAsync(new List<User> { new User { Email = email } });

//             // Act
//             var result = await _auth0UserService.IsEmailTakenAsync(email);

//             // Assert
//             Assert.True(result);
//         }

//         [Fact]
//         public async Task IsEmailTakenAsync_ShouldReturnFalse_WhenEmailDoesNotExist()
//         {
//             // Arrange
//             var email = "nonexistent@example.com";
//             _mockManagementApiClient
//                 .Setup(client => client.Users.GetAllAsync(It.Is<GetUsersRequest>(r => r.Query.Contains(email))))
//                 .ReturnsAsync(new List<User>());

//             // Act
//             var result = await _auth0UserService.IsEmailTakenAsync(email);

//             // Assert
//             Assert.False(result);
//         }
//     }
// }
