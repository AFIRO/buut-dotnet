using Rise.Domain.Users;
using Rise.Shared.Users;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Auth0.ManagementApi.Paging;
using Auth0.Core.Exceptions;
using Rise.Shared.Enums;
using Microsoft.Extensions.Logging;
namespace Rise.Services.Users;


/// <summary>
/// Service for managing Auth0 users.
/// </summary>
public class Auth0UserService : IAuth0UserService
{
    private readonly IManagementApiClient _managementApiClient;
    private readonly ILogger<Auth0UserService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Auth0UserService"/> class.
    /// </summary>
    /// <param name="managementApiClient">The Auth0 management API client.</param>
    /// <param name="logger">The logger instance.</param>
    public Auth0UserService(IManagementApiClient managementApiClient, ILogger<Auth0UserService> logger)
    {
        _managementApiClient = managementApiClient;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all users from Auth0.
    /// </summary>
    /// <returns>A collection of Auth0 users.</returns>
    public async Task<IEnumerable<UserDto.Auth0User>> GetAllUsersAsync()
    {
        try
        {
            // Fetch users from Auth0
            var users = await _managementApiClient.Users.GetAllAsync(new GetUsersRequest(), new PaginationInfo());

            // Transform and return the user list if successful
            var auth0Users = users.Select(x => new UserDto.Auth0User(
                x.Email,
                x.FirstName,
                x.LastName,
                x.Blocked ?? false
            ));
            _logger.LogInformation("Successfully fetched {Count} users from Auth0.", auth0Users.Count());
            return auth0Users;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Auth0 API error occurred while fetching all users.");
            throw new ExternalServiceException("ExternalServiceUnavailable", ex); // Localization key
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching all users from Auth0.");
            throw new ExternalServiceException("UnexpectedErrorOccurred", ex); // Localization key
        }
    }


    /// <summary>
    /// Retrieves a user from Auth0 by their ID.
    /// </summary>
    /// <param name="id">The ID of the user to retrieve.</param>
    /// <returns>An Auth0 user.</returns>
    public async Task<UserDto.Auth0User> GetUserByIdAsync(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Invalid user ID provided for fetching user by ID.");
                throw new ArgumentNullException(nameof(id), "User ID cannot be null or empty.");
            }
            // Fetch the user from Auth0
            var user = await _managementApiClient.Users.GetAsync(id);

            // Transform and return the user if successful
            var result = new UserDto.Auth0User(
                user.Email,
                user.FirstName,
                user.LastName,
                user.Blocked ?? false
            );
            _logger.LogInformation("Successfully fetched user with ID {UserId} from Auth0.", id);
            return result;

        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Invalid input provided.");
            throw;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Auth0 API error occurred while fetching user with ID {UserId}.", id);
            throw new ExternalServiceException("ExternalServiceUnavailable", ex); // Localization key
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching user with ID {UserId} from Auth0.", id);
            throw new ExternalServiceException("UnexpectedErrorOccurred", ex); // Localization key
        }
    }


    /// <summary>
    /// Registers a new user in Auth0.
    /// </summary>
    /// <param name="user">The user details for registration.</param>
    /// <returns>The registered user details.</returns>
    public async Task<UserDto.RegistrationUser> RegisterUserAuth0(UserDto.RegistrationUser user)
    {
        try
        {
            if (user is null)
            {
                _logger.LogWarning("Null user object provided for registration.");
                throw new ArgumentNullException(nameof(user), "User data cannot be null.");
            }
            // Check if the user already exists in Auth0
            var usersWithEmail = await _managementApiClient.Users.GetAllAsync(new GetUsersRequest { Query = $"email:\"{user.Email}\"" });
            if (usersWithEmail.Any())
            {
                _logger.LogWarning("User with email {Email} already exists in Auth0.", user.Email);
                throw new UserAlreadyExistsException("UserAlreadyExists"); // Localization key
            }
            var userCreateRequest = new UserCreateRequest
            {
                Email = user.Email,
                Password = user.Password,
                Connection = "Username-Password-Authentication",
                FirstName = user.FirstName,
                LastName = user.LastName,
            };

            var response = await _managementApiClient.Users.CreateAsync(userCreateRequest);

            // Fetch the "Pending" role ID
            var pendingRoleId = await GetAuth0RoleIdByEnum(RolesEnum.Pending) ?? throw new ExternalServiceException("PendingRoleNotFound", new Exception("The 'Pending' role was not found in Auth0."));
            _logger.LogInformation("Successfully fetched 'Pending' role ID.");
            // Assign the "Pending" role to the newly created user
            var assignRolesRequest = new AssignRolesRequest { Roles = new[] { pendingRoleId } }; // Assign the "Pending" role

            await _managementApiClient.Users.AssignRolesAsync(response.UserId, assignRolesRequest);
            _logger.LogInformation("Assigned 'Pending' role to user with ID {UserId}.", response.UserId);

            return new UserDto.RegistrationUser(response.FirstName, response.LastName, response.Email, user.PhoneNumber, null, response.UserId, user.Address, user.BirthDate);

        }
        catch (UserAlreadyExistsException ex)
        {
            _logger.LogWarning(ex, "User with email {Email} already exists.", user.Email);
            throw;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Auth0 API error occurred during user registration for email {Email}.", user.Email);
            throw new ExternalServiceException("ExternalServiceUnavailable", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during user registration for email {Email}.", user.Email);
            throw new ExternalServiceException("UnexpectedErrorOccurred", ex);
        }
    }

    /// <summary>
    /// Updates a user in Auth0.
    /// </summary>
    /// <param name="user">The user details to update.</param>
    /// <returns>True if the user was successfully updated, otherwise false.</returns>
    public async Task<bool> UpdateUserAuth0(UserDto.UpdateUser user)
    {
        try
        {
            if (user == null || string.IsNullOrWhiteSpace(user.Id))
            {
                _logger.LogWarning("Invalid user data provided for update.");
                throw new ArgumentNullException(nameof(user), "User data or ID cannot be null.");
            }

            _logger.LogInformation("Attempting to update user with ID {UserId} in Auth0.", user.Id);
            var userUpdateRequest = new UserUpdateRequest
            {
                Email = string.IsNullOrWhiteSpace(user.Email) ? null : user.Email,
                FirstName = string.IsNullOrWhiteSpace(user.FirstName) ? null : user.FirstName,
                LastName = string.IsNullOrWhiteSpace(user.LastName) ? null : user.LastName,
                Password = string.IsNullOrWhiteSpace(user.Password) ? null : user.Password,
                Blocked = false,
                EmailVerified = false
            };

            var response = await _managementApiClient.Users.UpdateAsync(user.Id, userUpdateRequest);
            _logger.LogInformation("Successfully updated user with ID {UserId} in Auth0.", user.Id);
            return response != null;
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Invalid input provided for user update.");
            throw;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Auth0 API error occurred during update for user ID {UserId}.", user.Id);
            throw new ExternalServiceException("ExternalServiceUnavailable", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during user update for user ID {UserId}.", user.Id);
            throw new ExternalServiceException("UnexpectedErrorOccurred", ex);
        }
    }

    /// <summary>
    /// Assigns roles to a user in Auth0.
    /// </summary>
    /// <param name="user">The user details including roles to assign.</param>
    /// <returns>True if the roles were successfully assigned, otherwise false.</returns>
    public async Task<bool> AssignRoleToUser(UserDto.UpdateUser user)
    {
        try
        {
            if (user is null || user.Roles is null || string.IsNullOrWhiteSpace(user.Id))
            {
                _logger.LogWarning("Invalid user data provided for role assignment.");
                throw new ArgumentNullException(nameof(user), "User data or ID cannot be null.");
            }
            // Fetch the Auth0 Role IDs for the roles in the UpdateUser object
            var auth0RoleIds = new List<string>();

            foreach (var role in user.Roles)
            {
                var roleId = await GetAuth0RoleIdByEnum(role.Name);
                if (roleId != null)
                {
                    auth0RoleIds.Add(roleId);
                }
            }

            if (!auth0RoleIds.Any())
            {
                _logger.LogWarning("No valid roles found to assign to user with ID {UserId}.", user.Id);
                throw new ArgumentException("No valid roles found to assign to the user.");
            }

            var assignRolesRequest = new AssignRolesRequest { Roles = auth0RoleIds.ToArray() }; // Convert the list to an array

            var pendingRoleId = await GetAuth0RoleIdByEnum(RolesEnum.Pending);
            var removeRolesRequest = new AssignRolesRequest { Roles = [pendingRoleId] };

            //remove pending role
            await _managementApiClient.Users.RemoveRolesAsync(user.Id, removeRolesRequest);
            await _managementApiClient.Users.AssignRolesAsync(user.Id, assignRolesRequest);

            _logger.LogInformation("Successfully assigned roles to user with ID {UserId}.", user.Id);
            return true;
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Invalid input provided for role assignment.");
            throw;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Auth0 API error occurred during role assignment for user ID {UserId}.", user.Id);
            throw new ExternalServiceException("Failed to assign role", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during role assignment for user ID {UserId}.", user.Id);
            throw new ExternalServiceException("Unexpected error occurred", ex);
        }
    }

    /// <summary>
    /// Soft deletes a user in Auth0 by updating the user's app_metadata.
    /// </summary>
    /// <param name="userId">The ID of the user to be soft deleted.</param>
    /// <returns>True if the user was successfully soft deleted, otherwise false.</returns>
    public async Task<bool> SoftDeleteAuth0UserAsync(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Invalid user ID provided for soft deletion.");
                throw new ArgumentNullException(nameof(userId), "User ID cannot be null or empty.");
            }
            // Retrieve the existing user to ensure it exists in Auth0
            var user = await _managementApiClient.Users.GetAsync(userId) ?? throw new Exception($"User with ID {userId} not found in Auth0.");

            // Update the app_metadata to mark the user as soft deleted
            var userUpdateRequest = new UserUpdateRequest
            {
                AppMetadata = new Dictionary<string, object>
            {
                { "softDeleted", true }
            }
            };

            var response = await _managementApiClient.Users.UpdateAsync(userId, userUpdateRequest);

            // Return true if the update was successful
            if (response != null)
            {
                _logger.LogInformation("Successfully soft deleted user with ID {UserId} in Auth0.", userId);
                return true;
            }

            _logger.LogWarning("Soft deletion failed for user with ID {UserId} in Auth0.", userId);
            return false;
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Invalid input provided for soft deletion.");
            throw;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Auth0 API error occurred while attempting to soft delete user with ID {UserId}.", userId);
            throw new ExternalServiceException("Failed to soft delete user in Auth0.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during soft deletion for user with ID {UserId}.", userId);
            throw new ExternalServiceException("Unexpected error occurred during soft delete.", ex);
        }
    }


    /// <summary>
    /// Checks if an email is already taken in Auth0.
    /// </summary>
    /// <param name="email">The email to check.</param>
    /// <returns>True if the email is taken, otherwise false.</returns>
    public async Task<bool> IsEmailTakenAsync(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Invalid email provided for email availability check.");
                throw new ArgumentNullException(nameof(email), "Email cannot be null or empty.");
            }
            // Check if any user exists with this email in Auth0
            var usersWithEmail = await _managementApiClient.Users.GetAllAsync(new GetUsersRequest { Query = $"email:\"{email}\"" });
            return usersWithEmail.Any();
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Invalid input provided for email availability check.");
            throw;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Auth0 API error occurred while checking email availability for {Email}.", email);
            throw new ExternalServiceException("Auth0 service is unavailable", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while checking email availability for {Email}.", email);
            throw new ExternalServiceException("Unexpected error occurred", ex);
        }
    }

    // Method to get Auth0 Role ID by RolesEnum
    private async Task<string?> GetAuth0RoleIdByEnum(RolesEnum role)
    {
        try
        {
            // Fetch all roles from Auth0
            var roles = await _managementApiClient.Roles.GetAllAsync(new GetRolesRequest());

            // Find the role ID that matches your RolesEnum value
            var roleName = role.ToString(); // Assuming your RolesEnum values match the Auth0 role names
            var auth0Role = roles.FirstOrDefault(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));

            if (auth0Role != null)
            {
                _logger.LogInformation("Successfully found Auth0 role ID for {Role}: {RoleId}.", role, auth0Role.Id);
                return auth0Role.Id;
            }

            _logger.LogWarning("No Auth0 role found for {Role}.", role);
            return null;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Auth0 API error occurred while fetching roles for {Role}.", role);
            throw new ExternalServiceException("Failed to fetch roles from Auth0", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching roles for {Role}.", role);
            throw new ExternalServiceException("Unexpected error occurred while fetching roles", ex);
        }
    }
}
