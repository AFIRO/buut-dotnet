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

    /// <summary>
    /// Initializes a new instance of the <see cref="UserController"/> class with the specified user service.
    /// </summary>
    /// <param name="userService">The user service that handles user operations.</param>
    /// <param name="auth0UserService">The user service that handles Auth0 user operations</param>
    /// <param name="validationService">The validation service that handles validation operations</param>
    /// <param name="eventDispatcher">The event dispatcher that handles event operations</param>

    public UserController(IUserService userService, IAuth0UserService auth0UserService, IValidationService validationService, IEventDispatcher eventDispatcher)
    {
        _userService = userService;
        _auth0UserService = auth0UserService;
        _validationService = validationService;
        _eventDispatcher = eventDispatcher;
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
                return NotFound(new { message = "No users found." });
            }

            return Ok(users);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
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
                return Forbid("Access denied. You are not authorized to access this user's details.");
            }
            var user = await _userService.GetUserByIdAsync(userid);

            if (user == null)
            {
                return NotFound(new { message = $"User with ID {userid} was not found." });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            return StatusCode(500,
                new { message = "An unexpected error occurred while fetching the user details."});
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
                return Forbid("Access denied. You are not authorized to access this user's details.");
            }
            var user = await _userService.GetUserDetailsByIdAsync(userid);

            if (user == null)
            {
                return NotFound(new { message = $"User with ID {userid} was not found." });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            return StatusCode(500,
                new { message = "An unexpected error occurred while fetching the user details."});
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
                return BadRequest(new { message }); // Localization key
            }
            if (userDb.Id == null || userDb.FirstName == null || userDb.LastName == null)
            {
                return BadRequest(new { message = "User registration data is incomplete." });
            }

            var userRegistrationEvent = new UserRegisteredEvent(userDb.Id, userDb.FirstName, userDb.LastName);
            await _eventDispatcher.DispatchAsync(userRegistrationEvent);

            return Ok(new { message }); // Localization key

        }
        catch (UserAlreadyExistsException)
        {
            return Conflict(new { message = "UserAlreadyExists" }); // Localization key
        }
        catch (DatabaseOperationException)
        {
            return StatusCode(500, new { message = "UserCreationFailed" }); // Localization key
        }
        catch (ExternalServiceException)
        {
            return StatusCode(503, new { message = "ExternalServiceUnavailable" }); // Localization key
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "UnexpectedErrorOccurred" }); // Localization key
        }
    }


    // /// <summary>
    // /// Updates an existing user asynchronously.
    // /// </summary>
    // /// <param name="userDetails">The <see cref="UserDto.UpdateUser"/> object containing updated user details.</param>
    // /// <returns><c>true</c> if the update is successful; otherwise, <c>false</c>.</returns>
    // [HttpPut]
    // [Authorize]
    // public async Task<IActionResult> Put(UserDto.UpdateUser userDetails)
    // {
    //     if (userDetails == null)
    //     {
    //         return BadRequest(new { message = "User details cannot be null." });
    //     }
    //     var user = await _userService.GetUserByIdAsync(userDetails.Id);
    //     var rolesAssigned = false;
    //     try
    //     {
    //         // Update the user in Auth0
    //         var userUpdatedInAuth0 = await _auth0UserService.UpdateUserAuth0(userDetails);
    //         if (!userUpdatedInAuth0)
    //         {
    //             return NotFound(new { message = "User not found in Auth0." });
    //         }

    //         if (userDetails.Roles != null)
    //         {
    //             // Assign new roles to the user in Auth0
    //             rolesAssigned = await _auth0UserService.AssignRoleToUser(userDetails);
    //             if (!rolesAssigned)
    //             {
    //                 return StatusCode(500, new { message = "Failed to assign roles to user in Auth0." });
    //             }
    //         }

    //         // Update the user in the local database
    //         var userUpdatedInDb = await _userService.UpdateUserAsync(userDetails);
    //         if (!userUpdatedInDb)
    //         {
    //             return NotFound(new { message = $"User with ID {userDetails.Id} was not found." });
    //         }

    //         if ((bool)(user?.Roles?.Contains(new RoleDto { Name = RolesEnum.Pending })))
    //         {
    //             var userUpdateEvent = new UserUpdatedEvent(user.Id, user.FirstName, user.LastName);
    //         }


    //         return Ok(new { message = "User updated successfully." });
    //     }
    //     catch (ApiException ex)
    //     {
    //         // Handle specific Auth0 API exceptions
    //         return StatusCode(503,
    //             new { message = "Auth0 service is unavailable. Please try again later.", detail = ex.Message });
    //     }
    //     catch (DatabaseOperationException ex)
    //     {
    //         // Handle specific exceptions related to the local database
    //         return StatusCode(500,
    //             new
    //             {
    //                 message = "An error occurred while updating the user in the local database.",
    //                 detail = ex.Message
    //             });
    //     }
    //     catch (ExternalServiceException ex)
    //     {
    //         // Handle custom exceptions from external services
    //         return StatusCode(503, new { message = ex.Message, detail = ex.InnerException?.Message });
    //     }
    //     catch (ArgumentException ex)
    //     {
    //         // Handle cases where the input arguments might be invalid
    //         return BadRequest(new { message = ex.Message });
    //     }
    //     catch (UnauthorizedAccessException ex)
    //     {
    //         // Handle unauthorized access exceptions
    //         return StatusCode(403, new { message = $"Access denied: {ex.Message}" });
    //     }
    //     catch (Exception ex)
    //     {
    //         // Handle any other unexpected errors
    //         return StatusCode(500,
    //             new { message = "An unexpected error occurred while updating the user.", detail = ex.Message });
    //     }
    // }

    [HttpPut]
    [Authorize]
    public async Task<IActionResult> Put(UserDto.UpdateUser userDetails)
    {
        if (userDetails == null)
        {
            return BadRequest(new { message = "User details cannot be null." });
        }

        // Get authenticated user's ID and roles
        var authenticatedUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

        // Check if the user is updating their own details or is an Admin
        var isAdmin = userRoles.Contains(RolesEnum.Admin.ToString());
        if (!isAdmin && userDetails.Id != authenticatedUserId)
        {
            return Forbid("Access denied. You are not authorized to update this user's details.");
        }

        // Prevent non-admins from updating roles
        if (!isAdmin && userDetails.Roles != null)
        {
            return BadRequest(new { message = "You are not authorized to update roles." });
        }

        // Ensure non-admins update only allowed fields
        var user = await _userService.GetUserByIdAsync(userDetails.Id);
        if (user == null)
        {
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

            var userUpdatedInDb = await _userService.UpdateUserAsync(updatedUserDetails);
            if (!userUpdatedInDb)
            {
                return NotFound(new { message = $"User with ID {userDetails.Id} was not found." });
            }
            // Trigger UserUpdatedEvent if the user is updating their own profile
            if (!isAdmin)
            {
                var userUpdateEvent = new UserUpdatedEvent(user.Id, user.FirstName, user.LastName);
                await _eventDispatcher.DispatchAsync(userUpdateEvent);
            }

            if (isAdmin && userDetails.Roles != null)
            {
                var existingRoles = user.Roles.Select(r => r.Name).ToList();
                var rolesAssigned = await _auth0UserService.AssignRoleToUser(userDetails);
                if (!rolesAssigned)
                {
                    return StatusCode(500, new { message = "Failed to assign roles to user in Auth0." });
                }

                // **Synchronize roles with local database**
                var roleUpdateSuccess = await _userService.UpdateUserRolesAsync(userDetails.Id, userDetails.Roles);
                if (!roleUpdateSuccess)
                {
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
                }

            }

            return Ok(new { message = "User updated successfully." });
        }
        catch (ApiException ex)
        {
            return StatusCode(503,
                new { message = "Auth0 service is unavailable. Please try again later.", detail = ex.Message });
        }
        catch (Exception ex)
        {
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
                return BadRequest(new { message = "User has active bookings" });
            }

            var user = await _userService.GetUserByIdAsync(userid);
            var deleted = await _userService.SoftDeleteUserAsync(userid);
            var result = await _auth0UserService.SoftDeleteAuth0UserAsync(userid);
            if (user is null || !deleted || !result)
            {
                return NotFound(new { message = $"User with ID {userid} was not found." });
            }

            var userDeletionEvent = new UserDeletedEvent(user.Id, user.FirstName, user.LastName);
            await _eventDispatcher.DispatchAsync(userDeletionEvent);

            return Ok(new { message = $"User with ID {userid} has been deleted successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = $"Access denied: {ex.Message}" });
        }
        catch (UserNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (DatabaseOperationException ex)
        {
            return StatusCode(500, new { message = "An error occurred while deleting the user.", detail = ex.Message });
        }
        catch (Exception ex)
        {
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

            return Ok(auth0Users);
        }
        catch (ApiException ex)
        {
            // Handle specific Auth0 API exceptions, like network or request issues
            return StatusCode(503,
                new { message = "Auth0 service is unavailable. Please try again later.", detail = ex.Message });
        }
        catch (Exception ex)
        {
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
                // Return 404 Not Found if the user does not exist
                return NotFound(new { message = $"User with ID {userid} was not found." });
            }

            return Ok(auth0User);
        }
        catch (ApiException ex)
        {
            // Handle specific Auth0 API exceptions
            return StatusCode(503,
                new { message = "Auth0 service is unavailable. Please try again later.", detail = ex.Message });
        }
        catch (Exception ex)
        {
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
            Console.WriteLine("Email taken: " + isTaken);

            return Ok(isTaken);
        }
        catch (ExternalServiceException ex)
        {
            // Handle custom exceptions from Auth0UserService
            return StatusCode(503, new { message = ex.Message, detail = ex.InnerException?.Message });
        }
        catch (Exception ex)
        {
            // Handle any other unexpected errors
            return StatusCode(500,
                new { message = "An unexpected error occurred while checking the email.", detail = ex.Message });
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
                return NotFound("No users found matching the given filters.");
            }


            return Ok(users);
        }
        catch (UnauthorizedAccessException ex)
        {
            // Handle specific unauthorized access exceptions if needed
            return Forbid($"Access denied: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            // Handle cases where the filter might have invalid arguments
            return BadRequest($"Invalid filter argument: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Log the exception (if you have a logging system)
            // _logger.LogError(ex, "An error occurred while getting filtered users.");

            // Return a generic error response
            return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
        }
    }

}