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
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions;


    public UserService(ApplicationDbContext dbContext, HttpClient httpClient)
    {
        this._dbContext = dbContext;
        this._httpClient = httpClient;
        this._jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        this._jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task<IEnumerable<UserDto.UserBase>?> GetAllAsync()
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

    public async Task<(bool Success, string? Message)> CreateUserAsync(UserDto.RegistrationUser userDetails)
    {
        try
        {
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
            entity.AddRole(new Role(RolesEnum.Pending));

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
    if (userDetails.Email != null) entity.Email = userDetails.Email;
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
                entity.Roles.Add(new Role(newRole.Name));
            }
        }
    }

    // Update the user entity in the database
    _dbContext.Users.Update(entity);
    int response = await _dbContext.SaveChangesAsync();

    return response > 0;
}

    public async Task<bool> DeleteUserAsync(string userid)
    {
        var entity = await _dbContext.Users.FindAsync(userid) ?? throw new Exception("User not found");

        entity.SoftDelete();
        _dbContext.Users.Update(entity);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<UserDto.Auth0User>> GetAuth0Users()
    {
        var users = await _httpClient.GetFromJsonAsync<IEnumerable<UserDto.Auth0User>>("user/auth/users");
        return users!;
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
                ExtractAdress(user),
                ExtractRoles(user),
                user.BirthDate
            );
    }

    /// <summary>
    /// Extracts list of roles from a User
    /// </summary>
    /// <param name="user"></param>
    /// <returns>ImmutableList<RoleDto></returns>
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
    /// <returns>ddressDto.GetAdress</returns>
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
}
