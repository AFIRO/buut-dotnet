using System.Collections.Immutable;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Rise.Persistence;
using Rise.Shared.Users;
using Rise.Domain.Users;
using Rise.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace Rise.Services.Users;

/// <summary>
/// Provides services for managing users.
/// </summary>
public class UserService : IUserService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UserService> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserService"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    public UserService(ApplicationDbContext dbContext, ILogger<UserService> logger)
    {
        this._dbContext = dbContext;
        this._logger = logger;
        this._jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Retrieves all users that are not marked as deleted.
    /// </summary>
    /// <returns>A collection of UserBase DTOs.</returns>
    public async Task<IEnumerable<UserDto.UserBase>?> GetAllAsync()
    {
        try
        {
            // Changed method so that DTO creation is out of the LINQ Query
            // You need to avoid using methods with optional parameters directly
            // in the LINQ query that EF is trying to translate
            var users = await _dbContext.Users
                .Include(x => x.Roles) // Ensure Roles are loaded (Eagerly loading)
                .Where(x => x.IsDeleted == false)
                .ToListAsync();

            if (users is null)
            {
                _logger.LogInformation("No users found in the database.");
                return null;
            }

            _logger.LogInformation("{count} users retrieved from the database.", users.Count);
            return users.Select(MapToUserBase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving all users.");
            throw new Exception("An unexpected error occurred while retrieving users.", ex);
        }
    }

    /// <summary>
    /// Retrieves a user by their ID.
    /// </summary>
    /// <param name="userid">The ID of the user to retrieve.</param>
    /// <returns>A UserBase DTO if the user is found; otherwise, null.</returns>
    public async Task<UserDto.UserBase?> GetUserByIdAsync(string userid)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userid))
            {
                _logger.LogWarning("Invalid user ID provided.");
                throw new ArgumentException("User ID cannot be null or empty.");
            }

            // Changed method so that DTO creation is out of the LINQ Query
            // You need to avoid using methods with optional parameters directly
            // in the LINQ query that EF is trying to translate
            var user = await _dbContext.Users
                .Include(x => x.Roles) // Ensure Roles are loaded (Eagerly loading)
                .FirstOrDefaultAsync(x => x.Id.Equals(userid) && !x.IsDeleted);

            if (user is null)
            {
                _logger.LogWarning("User with ID {userid} not found.", userid);
                return null;
            }

            _logger.LogInformation("User with ID {userid} retrieved from the database.", userid);
            return MapToUserBase(user);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid input provided for retrieving a user.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving user by ID.");
            throw new Exception("An unexpected error occurred while retrieving the user.", ex);
        }
    }

    /// <summary>
    /// Retrieves detailed information about a user by their ID.
    /// </summary>
    /// <param name="userid">The ID of the user to retrieve details for.</param>
    /// <returns>A UserDetails DTO if the user is found; otherwise, null.</returns>
    public async Task<UserDto.UserDetails?> GetUserDetailsByIdAsync(string userid)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userid))
            {
                _logger.LogWarning("Invalid user ID provided for details retrieval.");
                throw new ArgumentException("User ID cannot be null or empty.");
            }
            // Changed method so that DTO creation is out of the LINQ Query
            // You need to avoid using methods with optional parameters directly
            // in the LINQ query that EF is trying to translate
            var user = await _dbContext.Users
                .Include(x => x.Address)
                .Include(x => x.Roles) // Ensure Address is loaded (Eagerly loading)
                .FirstOrDefaultAsync(x => x.Id.Equals(userid) && !x.IsDeleted);

            if (user is null)
            {
                _logger.LogWarning("User details for ID {userid} not found.", userid);
                return null;
            }

            _logger.LogInformation("Details retrieved for user with ID {userid}.", userid);
            return MapToUserDetails(user);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid input provided for user details retrieval.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving user details.");
            throw new Exception("An unexpected error occurred while retrieving user details.", ex);
        }
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="userDetails">The details of the user to create.</param>
    /// <returns>A tuple indicating success and a message.</returns>
    public async Task<(bool Success, string? Message)> CreateUserAsync(UserDto.RegistrationUser userDetails)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userDetails.Email) ||
                !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(userDetails.Email))
            {
                _logger.LogWarning("Invalid email format for user creation.");
                return (false, "InvalidEmailFormat"); // Localization key
            }

            if (_dbContext.Users.Any(x => x.Email == userDetails.Email))
            {
                _logger.LogWarning("User with email {email} already exists.", userDetails.Email);
                return (false, "UserAlreadyExists"); // Localization key
            }

            var user = new User(
                id: userDetails.Id,
                firstName: userDetails.FirstName,
                lastName: userDetails.LastName,
                email: userDetails.Email,
                birthDate: userDetails.BirthDate ?? DateTime.UtcNow,
                address: new Address(
                    street: userDetails.Address.Street.ToString() ?? "",
                    houseNumber: userDetails.Address.HouseNumber ?? "",
                    bus: userDetails.Address.Bus
                    ),
                phoneNumber: userDetails.PhoneNumber
            );

            Role? pendingRole = await GetRoleByNameFromDBAsync(new Role(RolesEnum.Pending));
            if (pendingRole is null)
            {
                _logger.LogError("Role 'Pending' not found in the database.");
                throw new Exception("Pending role not found.");
            }
            user.AddRole(pendingRole);

            await _dbContext.Users.AddAsync(user);
            var response = await _dbContext.SaveChangesAsync();

            _logger.LogInformation("User with email {email} created successfully.", userDetails.Email);
            return (response > 0, "UserCreatedSuccess"); // Localization key
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database operation failed during user creation.");
            throw new DatabaseOperationException("UserCreationFailed", ex); // Localization key
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during user creation.");
            throw new ExternalServiceException("UnexpectedErrorOccurred", ex); // Localization key
        }
    }


    /// <summary>
    /// Updates the details of an existing user.
    /// </summary>
    /// <param name="userDetails">The details of the user to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    public async Task<bool> UpdateUserAsync(UserDto.UpdateUser userDetails)
    {
        try
        {
            if (userDetails is null)
            {
                _logger.LogWarning("Attempted to update a user with null details.");
                throw new ArgumentNullException(nameof(userDetails));
            }

            // Fetch the user user from the database, including the related roles
            var user = await _dbContext.Users
                .Include(u => u.Roles).Include(u => u.Address)
                .FirstOrDefaultAsync(u => u.Id == userDetails.Id)
                ?? throw new Exception("User not found");


            // Update user details only if they are provided
            if (userDetails.FirstName != null) user.FirstName = userDetails.FirstName;
            if (userDetails.LastName != null) user.LastName = userDetails.LastName;
            if (userDetails.BirthDate != null) user.BirthDate = (DateTime)userDetails.BirthDate;
            if (userDetails.PhoneNumber != null) user.PhoneNumber = userDetails.PhoneNumber;

            if (userDetails.Address != null)
            {
                if (userDetails.Address.Street != null)
                {
                    user.Address.Street = userDetails.Address.Street.ToString();
                }
                if (userDetails.Address.HouseNumber != null)
                {
                    user.Address.HouseNumber = userDetails.Address.HouseNumber;
                }
                if (userDetails.Address.Bus != null)
                {
                    user.Address.Bus = userDetails.Address.Bus;
                }
                // Handle null value for Bus field
                user.Address.Bus = userDetails.Address.Bus ?? user.Address.Bus;
            }

            // Update the user user in the database
            _dbContext.Users.Update(user);
            var response = await _dbContext.SaveChangesAsync();

            return response > 0;
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Invalid input provided for user update.");
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database operation failed during user update.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during user update.");
            throw;
        }
    }

    /// <summary>
    /// Deletes a user by their ID.
    /// </summary>
    /// <param name="userid">The ID of the user to delete.</param>
    /// <returns>A boolean indicating whether the deletion was successful.</returns>
    public async Task<bool> SoftDeleteUserAsync(string userid)
    {
        try
        {
            var entity = await _dbContext.Users.FindAsync(userid);
            if (entity is null)
            {
                _logger.LogWarning("User with ID {userid} not found for deletion.", userid);
                throw new UserNotFoundException($"User with ID {userid} not found.");
            }

            entity.SoftDelete();
            _dbContext.Users.Update(entity);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("User with ID {userid} soft deleted successfully.", userid);
            return true;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database operation failed during user soft deletion.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during user soft deletion.");
            throw;
        }
    }


    /// <summary>
    /// Updates the roles of a user.
    /// </summary>
    /// <param name="userId">The ID of the user to update roles for.</param>
    /// <param name="newRoles">The new roles to assign to the user.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    public async Task<bool> UpdateUserRolesAsync(string userId, ImmutableList<RoleDto> newRoles)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Invalid user ID provided for role update.");
                throw new ArgumentException("User ID cannot be null or empty.");
            }

            if (newRoles == null || !newRoles.Any())
            {
                _logger.LogWarning("No roles provided for user ID {userId}.", userId);
                return false;
            }
            var user = await _dbContext.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user is null)
            {
                _logger.LogWarning("User with ID {userId} not found for role update.", userId);
                return false; // User not found
            }

            bool rolesWereCleared = false;

            // If the existing roles include "Pending", clear roles first
            if (user.Roles.Any(r => r.Name == RolesEnum.Pending))
            {
                _logger.LogInformation("Clearing 'Pending' role for user ID {userId}.", userId);
                user.Roles.Clear();
                rolesWereCleared = true;
            }

            bool anyValidRolesAdded = false;

            // Add new roles
            foreach (var newRole in newRoles)
            {
                var roleEntity = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == newRole.Name);
                if (roleEntity is not null)
                {
                    // Avoid duplicating roles
                    if (!user.Roles.Contains(roleEntity))
                    {
                        _logger.LogInformation("Adding role {roleName} to user ID {userId}.", newRole.Name, userId);
                        user.Roles.Add(roleEntity);
                        anyValidRolesAdded = true;
                    }
                }
                else
                {
                    _logger.LogWarning("Role {roleName} not found in the database.", newRole.Name);
                }

            }

            // If no valid roles were added and roles were not cleared, return false
            if (!anyValidRolesAdded && !rolesWereCleared)
            {
                _logger.LogWarning("No valid roles were added for user ID {userId}.", userId);
                return false;
            }

            // Save changes
            _dbContext.Users.Update(user);
            var result = await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Roles updated for user with ID {userId}.", userId);
            return result > 0;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid input provided for role update.");
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database operation failed during role update for user ID {userId}.", userId);
            throw new Exception("An error occurred while updating roles in the database.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during role update for user ID {userId}.", userId);
            throw;
        }
    }




    /// <summary>
    /// Retrieves a list of Auth0 users.
    /// </summary>
    /// <returns>A collection of Auth0User DTOs.</returns>
    public Task<IEnumerable<UserDto.Auth0User>> GetAuth0Users()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Maps a User to a UserBase DTO file
    /// </summary>
    /// <param name="user"></param>
    /// <returns>UserDto.UserBase</returns>
    private UserDto.UserBase MapToUserBase(User user)
    {
        return new UserDto.UserBase
            (
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                ExtractRoles(user)
            );
    }

    /// <summary>
    /// Maps a User to a UserDetails DTO file
    /// </summary>
    /// <param name="user"></param>
    /// <returns>UserDto.UserDetails</returns>
    private UserDto.UserDetails MapToUserDetails(User user)
    {
        return new UserDto.UserDetails
            (
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber,
                ExtractAdress(user),
                ExtractRoles(user),
                user.BirthDate
            );
    }

    /// <summary>
    /// Extracts list of roles from a User
    /// </summary>
    /// <param name="user"></param>
    /// <returns>ImmutableList&lt;RoleDto&gt;</returns>
    private ImmutableList<RoleDto> ExtractRoles(User user)
    {
        return user.Roles.Select(r => new RoleDto
        {
            Name = (Shared.Enums.RolesEnum)r.Name
        }).ToImmutableList();
    }

    /// <summary>
    /// Extracts AdressDTO from a User
    /// </summary>
    /// <param name="user"></param>
    /// <returns>AddressDto.GetAdress</returns>
    private AddressDto.GetAdress ExtractAdress(User user)
    {
        return new AddressDto.GetAdress
        {
            Street = StreetEnumExtensions.GetStreetEnum(user.Address.Street),
            HouseNumber = user.Address.HouseNumber,
            Bus = user.Address.Bus
        };
    }

    private Address MapToAddress(AddressDto.GetAdress adress)
    {
        return new Address
        (
            StreetEnumExtensions.GetStreetName(adress.Street),
            adress.HouseNumber ?? "",
            adress.Bus
        );
    }

    /// <summary>
    /// Checks if the given email is already taken.
    /// </summary>
    /// <param name="email">The email to check.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the email is taken.</returns>
    public Task<Boolean> IsEmailTakenAsync(String email)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Retrieves a filtered list of users based on the provided filter criteria.
    /// </summary>
    /// <param name="filter">The filter criteria to apply.</param>
    /// <returns>A collection of UserBase DTOs that match the filter criteria.</returns>
    public async Task<IEnumerable<UserDto.UserBase>> GetFilteredUsersAsync(UserFilter filter)
    {
        try
        {
            // Validate the input filter
            if (filter is null)
            {
                _logger.LogWarning("Null filter provided for GetFilteredUsersAsync.");
                throw new ArgumentNullException(nameof(filter), "Filter cannot be null.");
            }

            // Start with the base query, including necessary relationships
            IQueryable<User> query = _dbContext.Users
                .Include(u => u.Roles)
                .Include(u => u.Address);

            // Use reflection to iterate through properties of the filter
            var filterProperties = typeof(UserFilter).GetProperties();
            foreach (var property in filterProperties)
            {
                var value = property.GetValue(filter);
                if (value == null) continue; // Skip if the filter value is null

                // Apply filters based on property names
                switch (property.Name)
                {
                    case nameof(UserFilter.FirstName):
                        query = query.Where(u => u.FirstName.Contains((string)value));
                        break;

                    case nameof(UserFilter.LastName):
                        query = query.Where(u => u.LastName.Contains((string)value));
                        break;

                    case nameof(UserFilter.Email):
                        query = query.Where(u => u.Email.Contains((string)value));
                        break;

                    case nameof(UserFilter.Role):
                        query = query.Where(u => u.Roles.Any(r => r.Name == (RolesEnum)value));
                        break;

                    case nameof(UserFilter.RegisteredAfter):
                        query = query.Where(u => u.CreatedAt >= (DateTime)value);
                        break;

                    case nameof(UserFilter.IsDeleted):
                        query = query.Where(u => u.IsDeleted == (bool)value);
                        break;
                        // Add more cases as needed for additional properties
                }
            }
            // If IsDeleted filter is not provided, exclude deleted users by default
            if (!filterProperties.Any(p => p.Name == nameof(UserFilter.IsDeleted) && p.GetValue(filter) != null))
            {
                query = query.Where(u => !u.IsDeleted);
            }


            // Execute the query and map the results to UserBase DTOs
            var users = await query.ToListAsync();
            if (users is null)
            {
                _logger.LogInformation("No users found matching the provided filter criteria.");
                return Enumerable.Empty<UserDto.UserBase>();
            }
            _logger.LogInformation("{count} users found matching the filter criteria.", users.Count);
            return users.Select(MapToUserBase).ToList();
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Filter argument was null.");
            throw;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid filter criteria provided.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while filtering users.");
            throw new Exception("An unexpected error occurred while retrieving filtered users.", ex);
        }
    }

    private Task<Role?> GetRoleByNameFromDBAsync(Role role)
    {
        return _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == role.Name);
    }
}
