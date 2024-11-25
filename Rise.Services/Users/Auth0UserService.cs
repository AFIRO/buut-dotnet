using Rise.Domain.Users;
using Rise.Shared.Users;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Auth0.ManagementApi.Paging;
using Auth0.Core.Exceptions;
using Rise.Shared.Enums;
namespace Rise.Services.Users;


public class Auth0UserService : IAuth0UserService
{
    private readonly IManagementApiClient _managementApiClient;

    public Auth0UserService(IManagementApiClient managementApiClient)
    {
        _managementApiClient = managementApiClient;
    }

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
            return auth0Users;
        }
        catch (ApiException ex)
        {
            throw new ExternalServiceException("ExternalServiceUnavailable", ex); // Localization key
        }
        catch (Exception ex)
        {
            throw new ExternalServiceException("UnexpectedErrorOccurred", ex); // Localization key
        }
    }


    public async Task<UserDto.Auth0User> GetUserByIdAsync(String id)
    {
        try
        {
            // Fetch the user from Auth0
            var user = await _managementApiClient.Users.GetAsync(id);

            // Transform and return the user if successful
            return new UserDto.Auth0User(
                user.Email,
                user.FirstName,
                user.LastName,
                user.Blocked ?? false
            );
        }
        catch (ApiException ex)
        {
            throw new ExternalServiceException("ExternalServiceUnavailable", ex); // Localization key
        }
        catch (Exception ex)
        {
            throw new ExternalServiceException("UnexpectedErrorOccurred", ex); // Localization key
        }
    }


    public async Task<UserDto.RegistrationUser> RegisterUserAuth0(UserDto.RegistrationUser user)
    {
        var userCreateRequest = new UserCreateRequest
        {
            Email = user.Email,
            Password = user.Password,
            Connection = "Username-Password-Authentication",
            FirstName = user.FirstName,
            LastName = user.LastName,
        };
        try
        {
            // Check if the user already exists in Auth0
            var usersWithEmail = await _managementApiClient.Users.GetAllAsync(new GetUsersRequest { Query = $"email:\"{user.Email}\"" });
            if (usersWithEmail.Any())
            {
                throw new UserAlreadyExistsException("UserAlreadyExists"); // Localization key
            }

            var response = await _managementApiClient.Users.CreateAsync(userCreateRequest);

            // Fetch the "Pending" role ID
            var pendingRoleId = await GetAuth0RoleIdByEnum(RolesEnum.Pending) ?? throw new ExternalServiceException("PendingRoleNotFound", new Exception("The 'Pending' role was not found in Auth0."));

            // Assign the "Pending" role to the newly created user
            var assignRolesRequest = new AssignRolesRequest
            {
                Roles = new[] { pendingRoleId } // Assign the "Pending" role
            };
            await _managementApiClient.Users.AssignRolesAsync(response.UserId, assignRolesRequest);


            // Console.WriteLine("Created user: " + response.UserId);
            return new UserDto.RegistrationUser(response.FirstName, response.LastName, response.Email, user.PhoneNumber, null, response.UserId, user.Address, user.BirthDate);

        }
        catch (UserAlreadyExistsException)
        {
            throw;
        }
        catch (ApiException ex)
        {
            throw new ExternalServiceException("ExternalServiceUnavailable", ex); // Localization key
        }
        catch (Exception ex)
        {
            throw new ExternalServiceException("UnexpectedErrorOccurred", ex); // Localization key
        }
    }

    public async Task<bool> UpdateUserAuth0(UserDto.UpdateUser user)
    {
        // // Create the UserUpdateRequest and set properties
        // var userUpdateRequest = new UserUpdateRequest
        // {
        //     Email = !string.IsNullOrWhiteSpace(user.Email) ? user.Email : null,
        //     FirstName = !string.IsNullOrWhiteSpace(user.FirstName) ? user.FirstName : null,
        //     LastName = !string.IsNullOrWhiteSpace(user.LastName) ? user.LastName : null,
        //     Password = !string.IsNullOrWhiteSpace(user.Password) ? user.Password : null,
        //     Blocked = false,
        //     EmailVerified = false,
        // };
        // Create the UserUpdateRequest and set properties only when they are provided
        var userUpdateRequest = new UserUpdateRequest();

        if (!string.IsNullOrWhiteSpace(user.Email))
            userUpdateRequest.Email = user.Email;

        if (!string.IsNullOrWhiteSpace(user.FirstName))
            userUpdateRequest.FirstName = user.FirstName;

        if (!string.IsNullOrWhiteSpace(user.LastName))
            userUpdateRequest.LastName = user.LastName;

        if (!string.IsNullOrWhiteSpace(user.Password))
            userUpdateRequest.Password = user.Password;

        // Fields like Blocked and EmailVerified are explicitly se
        userUpdateRequest.Blocked = false;
        userUpdateRequest.EmailVerified = false;
        try
        {
            var response = await _managementApiClient.Users.UpdateAsync(user.Id, userUpdateRequest);
            return response != null;
        }
        catch (ApiException ex)
        {
            throw new ExternalServiceException("ExternalServiceUnavailable", ex); // Localization key
        }
        catch (Exception ex)
        {
            throw new ExternalServiceException("UnexpectedErrorOccurred", ex); // Localization key
        }
    }

    public async Task<bool> AssignRoleToUser(UserDto.UpdateUser user)
    {
        try
        {
            // Fetch the Auth0 Role IDs for the roles in the UpdateUser object
            var auth0RoleIds = new List<string>();

            var pendingRoleId = await GetAuth0RoleIdByEnum(RolesEnum.Pending);

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
                throw new ArgumentException("No valid roles found to assign to the user.");
            }

            var assignRolesRequest = new AssignRolesRequest
            {
                Roles = auth0RoleIds.ToArray() // Convert the list to an array
            };

            var removeRolesRequest = new AssignRolesRequest
            {
                Roles = [pendingRoleId]
            };

            //remove pending role
            await _managementApiClient.Users.RemoveRolesAsync(user.Id, removeRolesRequest);

            await _managementApiClient.Users.AssignRolesAsync(user.Id, assignRolesRequest);
            return true;
        }
        catch (ApiException ex)
        {
            throw new ExternalServiceException("Failed to assign role", ex);
        }
        catch (Exception ex)
        {
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
            return response != null;
        }
        catch (ApiException ex)
        {
            throw new ExternalServiceException("Failed to soft delete user in Auth0.", ex);
        }
        catch (Exception ex)
        {
            throw new ExternalServiceException("Unexpected error occurred during soft delete.", ex);
        }
    }


    public async Task<bool> IsEmailTakenAsync(String email)
    {
        try
        {
            // Check if any user exists with this email in Auth0
            var usersWithEmail = await _managementApiClient.Users.GetAllAsync(new GetUsersRequest { Query = $"email:\"{email}\"" });
            return usersWithEmail.Any();
        }
        catch (ApiException ex)
        {
            throw new ExternalServiceException("Auth0 service is unavailable", ex); // Custom exception
        }
        catch (Exception ex)
        {
            throw new ExternalServiceException("Unexpected error occurred", ex); // Custom exception
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

            return auth0Role?.Id;
        }
        catch (ApiException ex)
        {
            throw new ExternalServiceException("Failed to fetch roles from Auth0", ex);
        }
        catch (Exception ex)
        {
            throw new ExternalServiceException("Unexpected error occurred while fetching roles", ex);
        }
    }
}
