using Microsoft.AspNetCore.Mvc;
using Rise.Domain.Users;
using Rise.Shared.Users;
using Microsoft.AspNetCore.Authorization;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Auth0.ManagementApi.Paging;
using Rise.Shared.Bookings;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Auth0.Core.Exceptions;
using Rise.Shared.Services;
using Rise.Services.Events;
using Rise.Services.Events.User;
using Rise.Shared.Enums;
using System.Security.Claims;

namespace Rise.Server.Controllers;

/// <summary>
/// API controller for managing user-related operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
// [Authorize(Roles = "Admin")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuth0UserService _auth0UserService;
    private readonly IValidationService _validationService;
    private readonly IEventDispatcher _eventDispatcher;
    private readonly ILogger<UserController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserController"/> class with the specified user service.
    /// </summary>
    /// <param name="userService">The user service that handles user operations.</param>
    /// <param name="auth0UserService">The user service that handles Auth0 user operations</param>
    /// <param name="validationService">The validation service that handles validation operations</param>
    /// <param name="eventDispatcher">The event dispatcher that handles event operations</param>
    /// <param name="logger">The logger that handles logging operations</param>

    public UserController(IUserService userService, IAuth0UserService auth0UserService, IValidationService validationService, IEventDispatcher eventDispatcher, ILogger<UserController> logger)
    {
        _userService = userService;
        _auth0UserService = auth0UserService;
        _validationService = validationService;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all users asynchronously.
    /// </summary>
    /// <returns>List of <see cref="UserDto"/> objects or <c>null</c> if no users are found.</returns>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = await _userService.GetAllAsync();

            if (users == null || !users.Any())
            {
                _logger.LogWarning("No users found.");
                return NotFound(new { message = "No users found." });
            }

            _logger.LogInformation("Successfully retrieved {count} users.", users.Count());
            return Ok(users);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized access: {message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while fetching all users.");
            return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a user by their ID asynchronously.
    /// </summary>
    /// <param name="userid">The ID of the user to retrieve.</param>
    /// <returns>The <see cref="UserDto"/> object or <c>null</c> if no user with the specified ID is found.</returns>
    [HttpGet("{userid}")]
    [Authorize]
    public async Task<IActionResult> Get(string userid)
    {
        try
        {
            // Get the authenticated user's ID and roles
            var authenticatedUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList() ?? new List<string>();

            // Check if the authenticated user has Admin role or is accessing their own details
            if (!userRoles.Contains(RolesEnum.Admin.ToString()) && userid != authenticatedUserId)
            {
                _logger.LogWarning("Unauthorized access attempt by user {userid}.", userid);
                return Forbid("Access denied.");
            }
            var user = await _userService.GetUserByIdAsync(userid);

            if (user == null)
            {
                _logger.LogWarning("User with ID {userid} not found.", userid);
                return NotFound(new { message = $"User with ID {userid} was not found." });
            }
            _logger.LogInformation("Retrieved user with ID {userid}.", userid);
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching user details.");
            return StatusCode(500, new { message = "An unexpected error occurred while fetching the user." });

        }
    }

    /// <summary>
    /// Retrieves detailed information about a user by their ID asynchronously.
    /// </summary>
    /// <param name="userid">The ID of the user to retrieve details for.</param>
    /// <returns>The detailed <see cref="UserDto.UserDetails"/> object or <c>null</c> if no user with the specified ID is found.</returns>
    [HttpGet("{userid}/details")]
    [Authorize]
    public async Task<IActionResult> GetDetails(string userid)
    {
        try
        {
            // Get the authenticated user's ID and roles
            var authenticatedUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

            // Check if the authenticated user has Admin role or is accessing their own details
            if (!userRoles.Contains(RolesEnum.Admin.ToString()) && userid != authenticatedUserId)
            {
                _logger.LogWarning("Unauthorized access to user details by user {userid}.", userid);
                return Forbid("Access denied.");
            }
            var user = await _userService.GetUserDetailsByIdAsync(userid);

            if (user == null)
            {
                _logger.LogWarning("User with ID {userid} was not found.", userid);
                return NotFound(new { message = $"User with ID {userid} was not found." });
            }

            _logger.LogInformation("Retrieved user details for user {userid}.", userid);
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while fetching the user details.");
            return StatusCode(500,
                new { message = "An unexpected error occurred while fetching the user details." });
        }
    }

    /// <summary>
    /// Creates a new user asynchronously.
    /// </summary>
    /// <param name="userDetails">The <see cref="UserDto.RegistrationUser"/> object containing user details to create.</param>
    /// <returns>The created <see cref="UserDto.RegistrationUser"/> object or <c>null</c> if the user creation fails.</returns>
    [HttpPost]
    public async Task<IActionResult> Post(UserDto.RegistrationUser userDetails)
    {
        try
        {
            // var userDb = await RegisterUserAuth0(userDetails);
            var userDb = await _auth0UserService.RegisterUserAuth0(userDetails);
            var (success, message) = await _userService.CreateUserAsync(userDb);

            if (!success)
            {
                _logger.LogWarning("User creation failed: {message}", message);
                return BadRequest(new { message }); // Localization key
            }
            if (userDb.Id == null || userDb.FirstName == null || userDb.LastName == null)
            {
                _logger.LogWarning("User registration data is incomplete.");
                return BadRequest(new { message = "User registration data is incomplete." });
            }

            var userRegistrationEvent = new UserRegisteredEvent(userDb.Id, userDb.FirstName, userDb.LastName);
            await _eventDispatcher.DispatchAsync(userRegistrationEvent);

            _logger.LogInformation("User created successfully with ID {userId}.", userDb.Id);
            return Ok(new { message });// Localization key

        }
        catch (UserAlreadyExistsException)
        {
            _logger.LogWarning("User already exists.");
            return Conflict(new { message = "UserAlreadyExists" }); // Localization key
        }
        catch (DatabaseOperationException)
        {
            _logger.LogError("User creation failed.");
            return StatusCode(500, new { message = "UserCreationFailed" }); // Localization key
        }
        catch (ExternalServiceException)
        {
            _logger.LogError("External service is unavailable.");
            return StatusCode(503, new { message = "ExternalServiceUnavailable" }); // Localization key
        }
        catch (Exception)
        {
            _logger.LogError("An unexpected error occurred.");
            return StatusCode(500, new { message = "UnexpectedErrorOccurred" }); // Localization key
        }
    }



    /// <summary>
    /// Updates user details asynchronously.
    /// </summary>
    /// <param name="userDetails">The <see cref="UserDto.UpdateUser"/> object containing user details to update.</param>
    /// <returns>An <see cref="IActionResult"/> indicating the result of the update operation.</returns>
    [HttpPut]
    [Authorize]
    public async Task<IActionResult> Put(UserDto.UpdateUser userDetails)
    {
        if (userDetails == null)
        {
            _logger.LogWarning("Update operation failed due to null user details.");
            return BadRequest(new { message = "User details cannot be null." });
        }

        // Get authenticated user's ID and roles
        var authenticatedUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

        // Check if the user is updating their own details or is an Admin
        var isAdmin = userRoles.Contains(RolesEnum.Admin.ToString());
        if (!isAdmin && userDetails.Id != authenticatedUserId)
        {
            _logger.LogWarning("Access denied: User is not authorized to update user details.");
            return Forbid("Access denied. You are not authorized to update this user's details.");
        }

        // Prevent non-admins from updating roles
        if (!isAdmin && userDetails.Roles != null)
        {
            _logger.LogWarning("Access denied: User is not authorized to update roles.");
            return BadRequest(new { message = "You are not authorized to update roles." });
        }

        // Ensure non-admins update only allowed fields
        var user = await _userService.GetUserByIdAsync(userDetails.Id);
        if (user == null)
        {
            _logger.LogWarning("User not found.");
            return NotFound(new { message = "User not found." });
        }

        try
        {
            // Update user details (only allowed fields for non-admins)
            var updatedUserDetails = new UserDto.UpdateUser
            {
                Id = userDetails.Id,
                FirstName = userDetails.FirstName,
                LastName = userDetails.LastName,
                PhoneNumber = userDetails.PhoneNumber,
                BirthDate = userDetails.BirthDate,
                Address = userDetails.Address, // Street, HouseNumber, Bus
            };
            var UserUpdatedInAuth0 = await _auth0UserService.UpdateUserAuth0(updatedUserDetails);
            if (!UserUpdatedInAuth0)
            {
                _logger.LogWarning("User update failed in Auth0.");
                return StatusCode(500, new { message = "Failed to update user in Auth0." });
            }
            var userUpdatedInDb = await _userService.UpdateUserAsync(updatedUserDetails);
            if (!userUpdatedInDb)
            {
                _logger.LogWarning("User update failed.");
                return NotFound(new { message = $"User with ID {userDetails.Id} was not found." });
            }
            // Trigger UserUpdatedEvent if the user is updating their own profile
            if (!isAdmin)
            {
                var userUpdateEvent = new UserUpdatedEvent(user.Id, user.FirstName, user.LastName);
                await _eventDispatcher.DispatchAsync(userUpdateEvent);
                _logger.LogInformation("Notification sent to Admins: User updated their profile.");
            }

            if (isAdmin && userDetails.Roles != null)
            {
                var existingRoles = user.Roles.Select(r => r.Name).ToList();
                var rolesAssigned = await _auth0UserService.AssignRoleToUser(userDetails);
                if (!rolesAssigned)
                {
                    _logger.LogWarning("Failed to assign roles to user in Auth0.");
                    return StatusCode(500, new { message = "Failed to assign roles to user in Auth0." });
                }

                // **Synchronize roles with local database**
                var roleUpdateSuccess = await _userService.UpdateUserRolesAsync(userDetails.Id, userDetails.Roles);
                if (!roleUpdateSuccess)
                {
                    _logger.LogWarning("Failed to update user roles in the local database.");
                    return StatusCode(500, new { message = "Failed to update user roles in the local database." });
                }

                // Check if existing role was "Pending" and new roles differ
                if (existingRoles.Contains(RolesEnum.Pending) && !userDetails.Roles.Any(r => r.Name == RolesEnum.Pending))
                {
                    var userValidationEvent = new UserValidationEvent(user.Id, user.FirstName, user.LastName);
                    await _eventDispatcher.DispatchAsync(userValidationEvent);
                }
                else
                    // Check if the roles have changed (for UserRoleUpdatedEvent)
                    if (!existingRoles.SequenceEqual(userDetails.Roles.Select(r => r.Name)))
                {
                    var userRoleUpdateEvent = new UserRoleUpdatedEvent(user.Id, user.FirstName, user.LastName, existingRoles, userDetails.Roles.Select(r => r.Name).ToList());
                    await _eventDispatcher.DispatchAsync(userRoleUpdateEvent);
                    _logger.LogInformation("Notification sent to User: Roles have been updated.");
                }

            }

            _logger.LogInformation("User updated successfully.");
            return Ok(new { message = "User updated successfully." });
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Auth0 service is unavailable.");
            return StatusCode(503,
                new { message = "Auth0 service is unavailable. Please try again later.", detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while updating the user.");
            return StatusCode(500,
                new { message = "An unexpected error occurred while updating the user.", detail = ex.Message });
        }
    }


    /// <summary>
    /// Deletes a user by their ID asynchronously.
    /// </summary>
    /// <param name="userid">The ID of the user to delete.</param>
    /// <returns><c>true</c> if the deletion is successful; otherwise, <c>false</c>.</returns>
    [HttpDelete("{userid}/softdelete")]
    [Authorize]
    public async Task<IActionResult> Delete(string userid)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userid))
            {
                return BadRequest(new { message = "User ID cannot be null or empty." });
            }

            var activeBookings = await _validationService.CheckActiveBookings(userid);
            if (activeBookings)
            {
                _logger.LogWarning("User with ID {userid} has active bookings.", userid);
                return BadRequest(new { message = "User has active bookings" });
            }

            var user = await _userService.GetUserByIdAsync(userid);
            var deleted = await _userService.SoftDeleteUserAsync(userid);
            var result = await _auth0UserService.SoftDeleteAuth0UserAsync(userid);
            if (user is null || !deleted || !result)
            {
                _logger.LogWarning("User with ID {userid} not found.", userid);
                return NotFound(new { message = $"User with ID {userid} was not found." });
            }

            var userDeletionEvent = new UserDeletedEvent(user.Id, user.FirstName, user.LastName);
            await _eventDispatcher.DispatchAsync(userDeletionEvent);


            _logger.LogInformation("User with ID {userid} deleted successfully.", userid);
            return Ok(new { message = $"User with ID {userid} has been deleted successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Access denied: {message}", ex.Message);
            return StatusCode(403, new { message = $"Access denied: {ex.Message}" });
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning("User not found: {message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (DatabaseOperationException ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the user.");
            return StatusCode(500, new { message = "An error occurred while deleting the user.", detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while deleting the user.");
            return StatusCode(500, new { message = "An unexpected error occurred while deleting the user.", detail = ex.Message });
        }
    }


    /// <summary>
    /// Retrieves all Auth0 users asynchronously.
    /// </summary>
    /// <returns>A list of Auth0 users.</returns>
    [HttpGet("authUsers")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUsers()
    {
        try
        {
            var auth0Users = await _auth0UserService.GetAllUsersAsync();

            _logger.LogInformation("Auth0 users found: {count}", auth0Users.Count());
            return Ok(auth0Users);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "An error occurred while fetching Auth0 users.");
            // Handle specific Auth0 API exceptions, like network or request issues
            return StatusCode(503,
                new { message = "Auth0 service is unavailable. Please try again later.", detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while fetching Auth0 users.");
            // Handle any unexpected errors and return a 500 Internal Server Error
            return StatusCode(500,
                new { message = "An unexpected error occurred while fetching users.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves an Auth0 user by their ID asynchronously.
    /// </summary>
    /// <param name="userid">The ID of the user to retrieve.</param>
    /// <returns>The Auth0 user object or a suitable error response if the user is not found or an error occurs.</returns>
    [HttpGet("auth/{userid}")]
    [Authorize]
    public async Task<IActionResult> GetUser(string userid)
    {
        try
        {
            var auth0User = await _auth0UserService.GetUserByIdAsync(userid);

            if (auth0User == null)
            {
                _logger.LogWarning("User with ID {userid} was not found.", userid);
                // Return 404 Not Found if the user does not exist
                return NotFound(new { message = $"User with ID {userid} was not found." });
            }

            return Ok(auth0User);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Auth0 service is unavailable.");
            // Handle specific Auth0 API exceptions
            return StatusCode(503,
                new { message = "Auth0 service is unavailable. Please try again later.", detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while fetching the user.");
            // Handle any other unexpected errors
            return StatusCode(500,
                new { message = "An unexpected error occurred while fetching the user.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Checks if an email is already taken asynchronously.
    /// </summary>
    /// <param name="email">The email to check.</param>
    /// <returns><c>true</c> if the email exists; otherwise, <c>false</c>.</returns>
    [HttpGet("exists")]
    [AllowAnonymous] // Allows anyone to call this method
    public async Task<IActionResult> IsEmailTaken([FromQuery] string email)
    {
        try
        {
            // Use the Auth0UserService to check if the email is taken
            bool isTaken = await _auth0UserService.IsEmailTakenAsync(email);

            return Ok(isTaken);
        }
        catch (ExternalServiceException ex)
        {
            _logger.LogError(ex, "An error occurred while checking the email.");
            // Handle custom exceptions from Auth0UserService
            return StatusCode(503, new { message = ex.Message, detail = ex.InnerException?.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while checking email existence.");
            // Handle any other unexpected errors
            return StatusCode(500,
                new { message = "An unexpected error occurred while checking email existence.", detail = ex.Message });
        }
    }


    /// <summary>
    /// Retrieves filtered users based on the provided filter asynchronously.
    /// </summary>
    /// <param name="filter">The filter criteria to apply when retrieving users.</param>
    /// <returns>A list of filtered users or a suitable error response if no users are found or an error occurs.</returns>
    [HttpGet("filtered")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetFilteredUsers([FromQuery] UserFilter filter)
    {

        try
        {
            // Attempt to get filtered users from the service
            var users = await _userService.GetFilteredUsersAsync(filter);

            // Check if users are found; return a suitable response
            if (users == null || !users.Any())
            {
                _logger.LogWarning("No users found matching the given filters.");
                return NotFound("No users found matching the given filters.");
            }

            return Ok(users);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Access denied: {message}", ex.Message);
            // Handle specific unauthorized access exceptions if needed
            return Forbid($"Access denied: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid filter argument: {message}", ex.Message);
            // Handle cases where the filter might have invalid arguments
            return BadRequest($"Invalid filter argument: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Log the exception (if you have a logging system)
            _logger.LogError(ex, "An error occurred while getting filtered users.");

            // Return a generic error response
            return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
        }
    }

}