using System.Collections.Immutable;
using System.ComponentModel;
using Castle.Core.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Rise.Domain.Users;
using Rise.Persistence;
using Rise.Services.Users;
using Rise.Shared.Enums;
using Rise.Shared.Users;

namespace Rise.Services.Tests.Users;

public class UserServiceTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserService _userService;
    private readonly Mock<ILogger<UserService>> _logger;

    public UserServiceTests()
    {
        // Set up in-memory database options
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _logger = new Mock<ILogger<UserService>>();
        _userService = new UserService(_dbContext, _logger.Object);
    }

    private User CreateUser(string id, string firstname, string lastname)
    {
        return new User
        (
            id,
            firstname,
            lastname,
            $"user{id}@example.com",
            DateTime.Now,
            new Address(StreetEnum.AFRIKALAAN.ToString(), id),
            "+3247845784"
        );
    }

    private User CreateUser(string id, string firstname)
    {
        return CreateUser(id, firstname, $"lastname_{id}");
    }

    private User CreateUser(string id)
    {
        return CreateUser(id, $"Doe_{id}", $"lastname_{id}");
    }

    private User CreateUser(int id)
    {
        return CreateUser(id.ToString(), $"Doe_{id}", $"lastname_{id}");
    }

    private UserDto.RegistrationUser CreateRegistrationUser(int id)
    {
        return CreateRegistrationUser(id.ToString());
    }

    private UserDto.RegistrationUser CreateRegistrationUser(string id)
    {
        return CreateRegistrationUser(id, $"{id}@gmail.com");
    }

    private UserDto.RegistrationUser CreateRegistrationUser(string id, string email)
    {
        return new UserDto.RegistrationUser
        (
            $"John_{id}",
            $"Doe_{id}",
            email,
            "+3247845784",
            "1234test",
            id,
            new AddressDto.GetAdress
            {
                Street = StreetEnum.AFRIKALAAN,
                HouseNumber = id,
                Bus = $"A{id}"
            },
            DateTime.UtcNow.AddYears(-30));
    }

    private async Task AddPendingRoleToDatabase()
    {
        var pendingRole = new Role(RolesEnum.Pending);
        await _dbContext.Roles.AddAsync(pendingRole);
        await _dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnUsers_WhenUsersExist()
    {
        // Arrange
        var user1 = CreateUser("1");
        var user2 = CreateUser("2");
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _userService.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, user => user.Email == "user1@example.com");
        Assert.Contains(result, user => user.Email == "user2@example.com");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmpty_WhenNoUsersExist()
    {
        // Arrange - Ensure no users exist in the database

        // Act
        var result = await _userService.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldNotReturnDeletedUsers()
    {
        // Arrange
        var user1 = CreateUser("1");
        var user2 = CreateUser("2");
        user2.IsDeleted = true;
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _userService.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.DoesNotContain(result, user => user.Email == "user2@example.com");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnUsersWithRoles_WhenUsersHaveRoles()
    {
        // Arrange
        var role = new Role { Name = RolesEnum.Admin };
        var user = CreateUser("1");
        user.Roles.Add(role);
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _userService.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        var userResult = result.FirstOrDefault();
        Assert.NotNull(userResult);
        Assert.Contains(userResult.Roles, r => r.Name == RolesEnum.Admin);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllUsers_WhenUsersAreAddedConcurrently()
    {
        // Arrange
        var user1 = CreateUser("1");
        var user2 = CreateUser("2");

        await Task.WhenAll(_dbContext.Users.AddAsync(user1).AsTask(), _dbContext.Users.AddAsync(user2).AsTask());
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _userService.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, user => user.Email == "user1@example.com");
        Assert.Contains(result, user => user.Email == "user2@example.com");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnUsers_WhenUsersHaveNoRoles()
    {
        // Arrange
        var user = CreateUser("1");
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _userService.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("user1@example.com", result.First().Email);
        Assert.Empty(result.First().Roles);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllUsers_WhenDatabaseContainsManyUsers()
    {
        // Arrange
        for (int i = 1; i <= 10000; i++)
        {
            var user = CreateUser(i);
            await _dbContext.Users.AddAsync(user);
        }

        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _userService.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10000, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnUsersWithEmptyRoles_WhenNoRolesAssigned()
    {
        // Arrange
        var user = CreateUser("1");
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _userService.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        var returnedUser = result.First();
        Assert.NotNull(returnedUser);
        Assert.Empty(returnedUser.Roles);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var user = CreateUser("1", "John_1", "Doe_1");
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _userService.GetUserByIdAsync("1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("1", result.Id);
        Assert.Equal("John_1", result.FirstName);
        Assert.Equal("Doe_1", result.LastName);
        Assert.Equal("user1@example.com", result.Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        // No user added to the database

        // Act
        var result = await _userService.GetUserByIdAsync("1");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnNull_WhenUserIsDeleted()
    {
        // Arrange
        var user = CreateUser("1");
        user.IsDeleted = true;
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _userService.GetUserByIdAsync("1");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnUserWithRoles_WhenUserHasRoles()
    {
        // Arrange
        var role = new Role { Name = RolesEnum.Admin };
        var user = CreateUser("1");
        user.Roles.Add(role);
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _userService.GetUserByIdAsync("1");

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result.Roles, r => r.Name == RolesEnum.Admin);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnUser_WhenUserHasNoRoles()
    {
        // Arrange
        var user = CreateUser("2");
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _userService.GetUserByIdAsync("2");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Roles);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldHandleInvalidUserId_WhenUserIdIsEmpty()
    {
        // Arrange
        var userId = string.Empty;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _userService.GetUserByIdAsync(userId));
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldHandleInvalidUserId_WhenUserIdIsNull()
    {
        // Arrange
        string userId = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _userService.GetUserByIdAsync(userId));
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnUser_WhenUserHasMultipleRoles()
    {
        // Arrange
        var role1 = new Role { Name = RolesEnum.Admin };
        var role2 = new Role { Name = RolesEnum.User };
        var user = CreateUser("3");
        user.Roles.Add(role1);
        user.Roles.Add(role2);
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _userService.GetUserByIdAsync("3");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Roles.Count);
        Assert.Contains(result.Roles, r => r.Name == RolesEnum.Admin);
        Assert.Contains(result.Roles, r => r.Name == RolesEnum.User);
    }

    [Fact]
    public async Task GetUserDetailsByIdAsync_ShouldReturnUserDetails_WhenUserExists()
    {
        // Arrange
        var user = CreateUser("1", "John_1", "Doe_1");
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _userService.GetUserDetailsByIdAsync("1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("1", result.Id);
        Assert.Equal("John_1", result.FirstName);
        Assert.Equal("Doe_1", result.LastName);
        Assert.Equal("user1@example.com", result.Email);
    }

    [Fact]
    public async Task GetUserDetailsByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = "non_existent";

        // Act
        var result = await _userService.GetUserDetailsByIdAsync(userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserDetailsByIdAsync_ShouldReturnNull_WhenUserIsDeleted()
    {
        // Arrange
        var user = CreateUser("2");
        user.IsDeleted = true;
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _userService.GetUserDetailsByIdAsync("2");

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetUserDetailsByIdAsync_ShouldReturnNull_WhenUserIdIsNullOrEmpty(string userId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _userService.GetUserDetailsByIdAsync(userId));
    }

    [Fact]
    public async Task GetUserDetailsByIdAsync_ShouldReturnUserDetails_WhenUserHasMultipleRoles()
    {
        // Arrange
        var user = CreateUser("4", "John_4", "Doe_4");
        user.Roles.Add(new Role { Name = RolesEnum.Admin });
        user.Roles.Add(new Role { Name = RolesEnum.User });
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _userService.GetUserDetailsByIdAsync("4");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("4", result.Id);
        Assert.Equal("John_4", result.FirstName);
        Assert.Equal("Doe_4", result.LastName);
        Assert.Equal("user4@example.com", result.Email);
        Assert.Contains(result.Roles, r => r.Name == RolesEnum.User);
        Assert.Contains(result.Roles, r => r.Name == RolesEnum.Admin);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldReturnSuccess_WhenUserCreatedSuccessfully()
    {
        // Arrange
        await AddPendingRoleToDatabase();
        var userDetails = CreateRegistrationUser("1");

        // Act
        var result = await _userService.CreateUserAsync(userDetails);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("UserCreatedSuccess", result.Message);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldReturnFailure_WhenUserAlreadyExists()
    {
        // Arrange
        await AddPendingRoleToDatabase();
        var userDetails = CreateRegistrationUser("1");
        await _dbContext.Users.AddAsync(new User
        (
            userDetails.Id,
            userDetails.FirstName,
            userDetails.LastName,
            userDetails.Email,
            userDetails.BirthDate ?? DateTime.UtcNow,
            new Address(
                street: userDetails.Address.Street.ToString() ?? "",
                houseNumber: userDetails.Address.HouseNumber ?? "",
                bus: userDetails.Address.Bus),
            userDetails.PhoneNumber
        ));
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _userService.CreateUserAsync(userDetails);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("UserAlreadyExists", result.Message);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldThrowExternalServiceException_WhenUnexpectedErrorOccurs()
    {
        // Arrange
        var userDetails = CreateRegistrationUser("1");

        // Act & Assert
        await Assert.ThrowsAsync<ExternalServiceException>(() => _userService.CreateUserAsync(userDetails));
    }

    [Fact]
    public async Task CreateUserAsync_ShouldThrowExternalServiceException_WhenRoleNotFound()
    {
        // Arrange
        var userDetails = CreateRegistrationUser("1");

        // Act & Assert
        await Assert.ThrowsAsync<ExternalServiceException>(() => _userService.CreateUserAsync(userDetails));
    }

    [Fact]
    public async Task CreateUserAsync_ShouldHandleConcurrency_WhenCreatingMultipleUsers()
    {
        // Arrange
        // Add a 'Pending' role to the in-memory database
        var pendingRole = new Role(RolesEnum.Pending);
        await _dbContext.Roles.AddAsync(pendingRole);
        await _dbContext.SaveChangesAsync();

        var userDetails1 = CreateRegistrationUser("1");
        var userDetails2 = CreateRegistrationUser("2");

        // Act
        var createUser1 = _userService.CreateUserAsync(userDetails1);
        var createUser2 = _userService.CreateUserAsync(userDetails2);
        await Task.WhenAll(createUser1, createUser2);

        // Assert
        var users = await _dbContext.Users.ToListAsync();
        Assert.Equal(2, users.Count);
    }


    [Fact]
    public async Task CreateUserAsync_ShouldThrowArgumentException_WhenUserDetailsInvalid()
    {
        // Arrange
        UserDto.RegistrationUser userDetails = null;

        // Act & Assert
        await Assert.ThrowsAsync<ExternalServiceException>(() => _userService.CreateUserAsync(userDetails));
    }

    [Fact]
    public async Task CreateUserAsync_ShouldReturnFailure_WhenEmailFormatInvalid()
    {
        // Arrange
        await AddPendingRoleToDatabase();
        var userDetails = CreateRegistrationUser("1", "invalid-email");

        // Act
        var result = await _userService.CreateUserAsync(userDetails);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("InvalidEmailFormat", result.Message);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldUpdateUserDetails_WhenValidDataIsProvided()
    {
        // Arrange
        var userId = "1";
        var originalUser = CreateUser(userId);
        var adminRole = new Role { Name = RolesEnum.Admin };

        // Add the Admin role to the database
        await _dbContext.Roles.AddAsync(adminRole);
        await _dbContext.Users.AddAsync(originalUser);
        await _dbContext.SaveChangesAsync();

        var updatedUserDetails = new UserDto.UpdateUser
        {
            Id = userId,
            FirstName = "UpdatedFirstName",
            LastName = "UpdatedLastName",
            BirthDate = new DateTime(1995, 5, 10),
            PhoneNumber = $"1234567890-{userId}",
            Address = new AddressDto.UpdateAddress()
            {
                Street = StreetEnum.AFRIKALAAN,
                HouseNumber = $"42",
                Bus = $"B-{userId}"
            },
            Roles = ImmutableList.Create(new RoleDto { Name = RolesEnum.Admin })
        };

        // Act
        var result = await _userService.UpdateUserAsync(updatedUserDetails);

        // Assert
        Assert.True(result);

        var updatedUser = await _dbContext.Users.Include(u => u.Roles).Include(u => u.Address)
            .FirstOrDefaultAsync(u => u.Id == userId);
        Assert.NotNull(updatedUser);
        Assert.Equal(updatedUserDetails.FirstName, updatedUser.FirstName);
        Assert.Equal(updatedUserDetails.LastName, updatedUser.LastName);
        Assert.Equal(updatedUserDetails.BirthDate, updatedUser.BirthDate);
        Assert.Equal(updatedUserDetails.PhoneNumber, updatedUser.PhoneNumber);
        Assert.Equal(updatedUserDetails.Address.Street.ToString().ToLower(), updatedUser.Address.Street.ToLower());
        Assert.Equal(updatedUserDetails.Address.HouseNumber, updatedUser.Address.HouseNumber);
        Assert.Equal(updatedUserDetails.Address.Bus, updatedUser.Address.Bus); ;
    }


    [Fact]
    public async Task UpdateUserAsync_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        var userDetails = new UserDto.UpdateUser
        {
            Id = "NonExistentUserId",
            FirstName = "UpdatedFirstName"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _userService.UpdateUserAsync(userDetails));
        Assert.Equal("User not found", exception.Message);
    }


    [Fact]
    public async Task UpdateUserAsync_ShouldUpdateUserAddress_WhenAddressProvided()
    {
        // Arrange
        var userId = "1";
        var originalUser = CreateUser(userId);
        await _dbContext.Users.AddAsync(originalUser);
        await _dbContext.SaveChangesAsync();

        var updatedUserDetails = new UserDto.UpdateUser
        {
            Id = userId,
            Address = new AddressDto.UpdateAddress()
            {
                Street = StreetEnum.AFRIKALAAN,
                HouseNumber = "221", // Updated to be a numeric value greater than zero
                Bus = $"A-{userId}"
            }
        };

        // Act
        var result = await _userService.UpdateUserAsync(updatedUserDetails);

        // Assert
        Assert.True(result);

        var updatedUser = await _dbContext.Users.Include(u => u.Address).FirstOrDefaultAsync(u => u.Id == userId);
        Assert.NotNull(updatedUser);
        Assert.Equal(updatedUserDetails.Address.Street.ToString().ToLower(), updatedUser.Address.Street.ToLower());
        Assert.Equal(updatedUserDetails.Address.HouseNumber, updatedUser.Address.HouseNumber);
        Assert.Equal(updatedUserDetails.Address.Bus, updatedUser.Address.Bus);
    }


    [Fact]
    public async Task UpdateUserAsync_ShouldNotChangeOtherFields_WhenPartialUpdateIsPerformed()
    {
        // Arrange
        var userId = "1";
        var originalUser = CreateUser(userId);
        await _dbContext.Users.AddAsync(originalUser);
        await _dbContext.SaveChangesAsync();

        var updatedUserDetails = new UserDto.UpdateUser
        {
            Id = userId,
            FirstName = "UpdatedFirstName"
        };

        // Act
        var result = await _userService.UpdateUserAsync(updatedUserDetails);

        // Assert
        Assert.True(result);

        var updatedUser = await _dbContext.Users.Include(u => u.Roles).Include(u => u.Address)
            .FirstOrDefaultAsync(u => u.Id == userId);
        Assert.NotNull(updatedUser);
        Assert.Equal(updatedUserDetails.FirstName, updatedUser.FirstName);
        Assert.Equal(originalUser.LastName, updatedUser.LastName);
        Assert.Equal(originalUser.Email, updatedUser.Email);
        Assert.Equal(originalUser.BirthDate, updatedUser.BirthDate);
        Assert.Equal(originalUser.PhoneNumber, updatedUser.PhoneNumber);
        Assert.Equal(originalUser.Address.Street, updatedUser.Address.Street);
        Assert.Equal(originalUser.Address.HouseNumber, updatedUser.Address.HouseNumber);
        Assert.Equal(originalUser.Address.Bus, updatedUser.Address.Bus);
        Assert.Equal(originalUser.Roles.Count, updatedUser.Roles.Count);
    }


    [Fact]
    public async Task UpdateUserAsync_ShouldHandleConcurrency_WhenMultipleUpdatesOccurSimultaneously()
    {
        // Arrange
        var userId = "1";
        var originalUser = CreateUser(userId);
        await _dbContext.Users.AddAsync(originalUser);
        await _dbContext.SaveChangesAsync();

        var updatedUserDetails1 = new UserDto.UpdateUser
        {
            Id = userId,
            FirstName = "UpdatedFirstName1"
        };

        var updatedUserDetails2 = new UserDto.UpdateUser
        {
            Id = userId,
            LastName = "UpdatedLastName2"
        };

        // Act
        var updateTask1 = _userService.UpdateUserAsync(updatedUserDetails1);
        var updateTask2 = _userService.UpdateUserAsync(updatedUserDetails2);

        await Task.WhenAll(updateTask1, updateTask2);

        // Assert
        var updatedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        Assert.NotNull(updatedUser);
        Assert.True(updatedUser.FirstName == "UpdatedFirstName1" || updatedUser.LastName == "UpdatedLastName2");
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldDeleteUser_WhenUserExists()
    {
        // Arrange
        var userId = "1";
        var user = CreateUser(userId);
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _userService.SoftDeleteUserAsync(userId);

        // Assert
        Assert.True(result);
        var deletedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        Assert.NotNull(deletedUser);
        Assert.True(deletedUser.IsDeleted);
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldThrowUserNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var nonExistentUserId = "NonExistentUserId";

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<UserNotFoundException>(() => _userService.SoftDeleteUserAsync(nonExistentUserId));
        Assert.Equal($"User with ID {nonExistentUserId} not found.", exception.Message);
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldReturnTrue_WhenUserIsAlreadyDeleted()
    {
        // Arrange
        var userId = "1";
        var user = CreateUser(userId);
        user.SoftDelete();
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _userService.SoftDeleteUserAsync(userId);

        // Assert
        Assert.True(result);
        var deletedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        Assert.NotNull(deletedUser);
        Assert.True(deletedUser.IsDeleted);
    }

    [Fact]
    public async Task GetFilteredUsersAsync_ShouldReturnUsers_WhenFilterIsApplied()
    {
        // Arrange
        var user1 = CreateUser("1", "User1");
        var user2 = CreateUser("2");
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        var filter = new UserFilter { FirstName = "User1" };

        // Act
        var result = await _userService.GetFilteredUsersAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(user1.Id, result.First().Id);
    }

    [Fact]
    public async Task GetFilteredUsersAsync_ShouldReturnUsers_WhenMultipleFiltersAreApplied()
    {
        // Arrange
        var user1 = CreateUser("1", "User", "One");
        var user2 = CreateUser("2");
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        var filter = new UserFilter { FirstName = "User", LastName = "One" };

        // Act
        var result = await _userService.GetFilteredUsersAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(user1.Id, result.First().Id);
    }

    [Fact]
    public async Task GetFilteredUsersAsync_ShouldReturnEmptyList_WhenNoUsersMatchFilter()
    {
        // Arrange
        var user1 = CreateUser("1");
        var user2 = CreateUser("2");
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        var filter = new UserFilter { FirstName = "NonExistent" };

        // Act
        var result = await _userService.GetFilteredUsersAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFilteredUsersAsync_ShouldExcludeDeletedUsers_WhenIsDeletedFilterNotProvided()
    {
        // Arrange
        var user1 = CreateUser("1");
        var user2 = CreateUser("2");
        user2.SoftDelete();
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        var filter = new UserFilter();

        // Act
        var result = await _userService.GetFilteredUsersAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(user1.Id, result.First().Id);
    }

    [Fact]
    public async Task GetFilteredUsersAsync_ShouldReturnDeletedUsers_WhenIsDeletedFilterIsProvided()
    {
        // Arrange
        var user1 = CreateUser("1");
        var user2 = CreateUser("2");
        user2.SoftDelete();
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        var filter = new UserFilter { IsDeleted = true };

        // Act
        var result = await _userService.GetFilteredUsersAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(user2.Id, result.First().Id);
    }

    [Fact]
    public async Task GetFilteredUsersAsync_ShouldReturnUsers_WhenRoleFilterIsApplied()
    {
        // Arrange
        var user1 = CreateUser("1");
        var user2 = CreateUser("2");
        user2.AddRole(new Role(RolesEnum.Admin));
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        var filter = new UserFilter { Role = RolesEnum.Admin };

        // Act
        var result = await _userService.GetFilteredUsersAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(user2.Id, result.First().Id);
    }

    [Fact]
    public async Task UpdateUserRolesAsync_ShouldUpdateRoles_WhenValidDataProvided()
    {
        // Arrange
        var userId = "1";
        var originalUser = CreateUser(userId);

        // Add roles to the database
        var adminRole = new Role { Name = RolesEnum.Admin };
        var userRole = new Role { Name = RolesEnum.User };
        await _dbContext.Roles.AddRangeAsync(adminRole, userRole);
        await _dbContext.Users.AddAsync(originalUser);
        await _dbContext.SaveChangesAsync();

        // Create new roles
        var newRoles = ImmutableList.Create(
            new RoleDto { Name = RolesEnum.Admin },
            new RoleDto { Name = RolesEnum.User });

        // Act
        var result = await _userService.UpdateUserRolesAsync(userId, newRoles);

        // Assert
        Assert.True(result);

        var updatedUser = await _dbContext.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == userId);
        Assert.NotNull(updatedUser);
        Assert.Equal(2, updatedUser.Roles.Count);
        Assert.Contains(updatedUser.Roles, r => r.Name == RolesEnum.Admin);
        Assert.Contains(updatedUser.Roles, r => r.Name == RolesEnum.User);
    }

    [Fact]
    public async Task UpdateUserRolesAsync_ShouldReturnFalse_WhenUserDoesNotExist()
    {
        // Arrange
        var nonExistentUserId = "NonExistentUserId";
        var newRoles = ImmutableList.Create(new RoleDto { Name = RolesEnum.Admin });

        // Act
        var result = await _userService.UpdateUserRolesAsync(nonExistentUserId, newRoles);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateUserRolesAsync_ShouldNotClearRoles_WhenEmptyRolesListProvided()
    {
        // Arrange
        var userId = "1";
        var originalUser = CreateUser(userId);
        var adminRole = new Role { Name = RolesEnum.Admin };
        originalUser.Roles.Add(adminRole);

        await _dbContext.Roles.AddAsync(adminRole);
        await _dbContext.Users.AddAsync(originalUser);
        await _dbContext.SaveChangesAsync();

        var newRoles = ImmutableList<RoleDto>.Empty;

        // Act
        var result = await _userService.UpdateUserRolesAsync(userId, newRoles);

        // Assert
        Assert.False(result); // Expect the update to fail because no valid roles were provided

        var updatedUser = await _dbContext.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == userId);
        Assert.NotNull(updatedUser);
        Assert.Single(updatedUser.Roles); // The existing role should remain
        Assert.Contains(updatedUser.Roles, r => r.Name == RolesEnum.Admin);
    }


    [Fact]
    public async Task UpdateUserRolesAsync_ShouldIgnoreNonexistentRoles()
    {
        // Arrange
        var userId = "1";
        var originalUser = CreateUser(userId);
        var adminRole = new Role { Name = RolesEnum.Admin };

        await _dbContext.Roles.AddAsync(adminRole);
        await _dbContext.Users.AddAsync(originalUser);
        await _dbContext.SaveChangesAsync();

        // Create a mix of existing and nonexistent roles
        var newRoles = ImmutableList.Create(
            new RoleDto { Name = RolesEnum.Admin },
            new RoleDto { Name = (RolesEnum)9999 }); // Invalid role

        // Act
        var result = await _userService.UpdateUserRolesAsync(userId, newRoles);

        // Assert
        Assert.True(result);

        var updatedUser = await _dbContext.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == userId);
        Assert.NotNull(updatedUser);
        Assert.Single(updatedUser.Roles);
        Assert.Contains(updatedUser.Roles, r => r.Name == RolesEnum.Admin);
    }

    [Fact]
    public async Task UpdateUserRolesAsync_ShouldReturnFalse_WhenNoRolesExistInDatabase()
    {
        // Arrange
        var userId = "1";
        var originalUser = CreateUser(userId);
        await _dbContext.Users.AddAsync(originalUser);
        await _dbContext.SaveChangesAsync();

        // Create new roles that don't exist in the database
        var newRoles = ImmutableList.Create(new RoleDto { Name = RolesEnum.Admin });

        // Act
        var result = await _userService.UpdateUserRolesAsync(userId, newRoles);

        // Assert
        Assert.False(result);

        var updatedUser = await _dbContext.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == userId);
        Assert.NotNull(updatedUser);
        Assert.Empty(updatedUser.Roles);
    }

    [Fact]
    public async Task UpdateUserRolesAsync_ShouldHandleConcurrency_WhenMultipleUpdatesOccurSimultaneously()
    {
        // Arrange
        var userId = "1";
        var originalUser = CreateUser(userId);
        var adminRole = new Role { Name = RolesEnum.Admin };
        var userRole = new Role { Name = RolesEnum.User };

        await _dbContext.Roles.AddRangeAsync(adminRole, userRole);
        await _dbContext.Users.AddAsync(originalUser);
        await _dbContext.SaveChangesAsync();

        var newRoles1 = ImmutableList.Create(new RoleDto { Name = RolesEnum.Admin });
        var newRoles2 = ImmutableList.Create(new RoleDto { Name = RolesEnum.User });

        // Act
        var updateTask1 = _userService.UpdateUserRolesAsync(userId, newRoles1);
        var updateTask2 = _userService.UpdateUserRolesAsync(userId, newRoles2);
        await Task.WhenAll(updateTask1, updateTask2);

        // Assert
        var updatedUser = await _dbContext.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == userId);
        Assert.NotNull(updatedUser);
        Assert.Equal(2, updatedUser.Roles.Count); // Both roles should exist
        Assert.Contains(updatedUser.Roles, r => r.Name == RolesEnum.Admin);
        Assert.Contains(updatedUser.Roles, r => r.Name == RolesEnum.User);
    }

}