using System.Collections.Immutable;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Rise.Persistence;
using Rise.Shared.Users;
using Rise.Domain.Users;
using Rise.Shared.Enums;

namespace Rise.Services.Users;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly JsonSerializerOptions _jsonSerializerOptions;


    /// <summary>
    /// Initializes a new instance of the <see cref="UserService"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="jsonSerializerOptions">The JSON serializer options.</param>
    public UserService(ApplicationDbContext dbContext)
    {
        this._dbContext = dbContext;
        // this._httpClient = httpClient;
        this._jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Retrieves all users that are not marked as deleted.
    /// </summary>
    /// <returns>A collection of UserBase DTOs.</returns>
    public async Task<IEnumerable<UserDto.UserBase>> GetAllAsync()
    {
        // Changed method so that DTO creation is out of the LINQ Query
        // You need to avoid using methods with optional parameters directly
        // in the LINQ query that EF is trying to translate
        var query = await _dbContext.Users
            .Include(x => x.Roles) // Ensure Roles are loaded (Eagerly loading)
            .Where(x => x.IsDeleted == false)
            .ToListAsync();

        if (query == null)
        {
            return null;
        }

        return query.Select(MapToUserBase);
    }

    /// <summary>
    /// Retrieves a user by their ID.
    /// </summary>
    /// <param name="userid">The ID of the user to retrieve.</param>
    /// <returns>A UserBase DTO if the user is found; otherwise, null.</returns>
    public async Task<UserDto.UserBase?> GetUserByIdAsync(string userid)
    {
        // Changed method so that DTO creation is out of the LINQ Query
        // You need to avoid using methods with optional parameters directly
        // in the LINQ query that EF is trying to translate
        var query = await _dbContext.Users
            .Include(x => x.Roles) // Ensure Roles are loaded (Eagerly loading)
            .FirstOrDefaultAsync(x => x.Id.Equals(userid) && x.IsDeleted == false);

        if (query == null)
        {
            return null;
        }

        return MapToUserBase(query);
    }

    /// <summary>
    /// Retrieves detailed information about a user by their ID.
    /// </summary>
    /// <param name="userid">The ID of the user to retrieve details for.</param>
    /// <returns>A UserDetails DTO if the user is found; otherwise, null.</returns>
    public async Task<UserDto.UserDetails?> GetUserDetailsByIdAsync(string userid)
    {
        // Changed method so that DTO creation is out of the LINQ Query
        // You need to avoid using methods with optional parameters directly
        // in the LINQ query that EF is trying to translate
        var query = await _dbContext.Users
            .Include(x => x.Address)
            .Include(x => x.Roles) // Ensure Address is loaded (Eagerly loading)
            .FirstOrDefaultAsync(x => x.Id.Equals(userid) && x.IsDeleted == false);

        if (query == null)
        {
            return null;
        }

        return MapToUserDetails(query);
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
                return (false, "InvalidEmailFormat"); // Localization key
            }

            if (_dbContext.Users.Any(x => x.Email == userDetails.Email))
            {
                return (false, "UserAlreadyExists"); // Localization key
            }

            var entity = new User(
                id: userDetails.Id,
                firstName: userDetails.FirstName,
                lastName: userDetails.LastName,
                email: userDetails.Email,
                birthDate: userDetails.BirthDate ?? DateTime.UtcNow,
                address: new Address(
                    street: userDetails.Address.Street.ToString() ?? "",
                    houseNumber: userDetails.Address.HouseNumber ?? "",
                    bus: userDetails.Address.Bus),
                phoneNumber: userDetails.PhoneNumber
            );
            Role pending = await GetRoleByNameFromDBAsync(new Role(RolesEnum.Pending)) ?? throw new Exception("Role not found");
            entity.AddRole(pending);

            _dbContext.Users.Add(entity);
            Int32 response = await _dbContext.SaveChangesAsync();

            return (response > 0, "UserCreatedSuccess"); // Localization key
        }
        catch (DbUpdateException ex)
        {
            throw new DatabaseOperationException("UserCreationFailed", ex); // Localization key
        }
        catch (Exception ex)
        {
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
        // Fetch the user entity from the database, including the related roles
        var entity = await _dbContext.Users
            .Include(u => u.Roles).Include(u => u.Address)
            .FirstOrDefaultAsync(u => u.Id == userDetails.Id)
            ?? throw new Exception("User not found");

        // Update user details only if they are provided
        if (userDetails.FirstName != null) entity.FirstName = userDetails.FirstName;
        if (userDetails.LastName != null) entity.LastName = userDetails.LastName;
        if (userDetails.BirthDate != null) entity.BirthDate = (DateTime)userDetails.BirthDate;
        if (userDetails.PhoneNumber != null) entity.PhoneNumber = userDetails.PhoneNumber;

        if (userDetails.Address != null)
        {
            if (userDetails.Address.Street != null)
            {
                entity.Address.Street = userDetails.Address.Street.ToString();
            }
            if (userDetails.Address.HouseNumber != null)
            {
                entity.Address.HouseNumber = userDetails.Address.HouseNumber;
            }
            if (userDetails.Address.Bus != null)
            {
                entity.Address.Bus = userDetails.Address.Bus;
            }
        }

        // Update roles only if they are provided
        if (userDetails.Roles != null)
        {
            // Get the list of current roles
            var currentRoleNames = entity.Roles.Select(r => r.Name).ToList();

            // Remove roles that are no longer in the new list
            var rolesToRemove = entity.Roles.Where(r => !userDetails.Roles.Any(newRole => newRole.Name == r.Name)).ToList();
            foreach (var roleToRemove in rolesToRemove)
            {
                entity.Roles.Remove(roleToRemove);
            }

            // Add new roles that are not already present
            foreach (var newRole in userDetails.Roles)
            {
                if (!currentRoleNames.Contains(newRole.Name))
                {
                    Role DBrole = await GetRoleByNameFromDBAsync(new Role(newRole.Name)) ?? throw new Exception("Role not found");
                    entity.Roles.Add(DBrole);
                }
            }
        }

        // Update the user entity in the database
        _dbContext.Users.Update(entity);
        int response = await _dbContext.SaveChangesAsync();

        return response > 0;
    }

    /// <summary>
    /// Deletes a user by their ID.
    /// </summary>
    /// <param name="userid">The ID of the user to delete.</param>
    /// <returns>A boolean indicating whether the deletion was successful.</returns>
    public async Task<bool> SoftDeleteUserAsync(string userid)
    {
        var entity = await _dbContext.Users.FindAsync(userid) ?? throw new UserNotFoundException($"User with ID {userid} not found.");
        entity.SoftDelete();
        _dbContext.Users.Update(entity);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Updates the roles of a user.
    /// </summary>
    /// <param name="userId">The ID of the user to update roles for.</param>
    /// <param name="newRoles">The new roles to assign to the user.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    public async Task<bool> UpdateUserRolesAsync(string userId, ImmutableList<RoleDto> newRoles)
    {
        var user = await _dbContext.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            return false; // User not found
        }

        bool rolesWereCleared = false;

        // If the existing roles include "Pending", clear roles first
        if (user.Roles.Any(r => r.Name == RolesEnum.Pending))
        {
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
                    user.Roles.Add(roleEntity);
                    anyValidRolesAdded = true;
                }
            }
        }

        // If no valid roles were added and roles were not cleared, return false
        if (!anyValidRolesAdded && !rolesWereCleared)
        {
            return false;
        }

        // Save changes
        _dbContext.Users.Update(user);
        return await _dbContext.SaveChangesAsync() > 0;
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
        return users.Select(MapToUserBase).ToList();
    }

    private Task<Role?> GetRoleByNameFromDBAsync(Role role)
    {
        return _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == role.Name);
    }
}
