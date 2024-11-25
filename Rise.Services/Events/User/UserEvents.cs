using Auth0.ManagementApi.Clients;
using Rise.Shared.Enums;

namespace Rise.Services.Events.User;

/// <summary>
/// Event triggered when a user is registered.
/// </summary>
/// <param name="userId">The user ID.</param>
/// <param name="firstName">The first name of the user.</param>
/// <param name="lastName">The last name of the user.</param>
public class UserRegisteredEvent(string userId, string firstName, string lastName) : IEvent
{
    /// <summary>
    /// Gets the user ID.
    /// </summary>
    public string UserId { get; } = userId;

    /// <summary>
    /// Gets the first name of the user.
    /// </summary>
    public string FirstName { get; } = firstName;

    /// <summary>
    /// Gets the last name of the user.
    /// </summary>
    public string LastName { get; } = lastName;
}


/// <summary>
/// Event triggered when a user is deleted.
/// </summary>
/// <param name="userId">The user ID.</param>
/// <param name="firstName">The first name of the user.</param>
/// <param name="lastName">The last name of the user.</param>
public class UserDeletedEvent(string userId, string firstName, string lastName) : IEvent
{
    /// <summary>
    /// Gets the user ID.
    /// </summary>
    public string UserId { get; } = userId;

    /// <summary>
    /// Gets the first name of the user.
    /// </summary>
    public string FirstName { get; } = firstName;

    /// <summary>
    /// Gets the last name of the user.
    /// </summary>
    public string LastName { get; } = lastName;
}

/// <summary>
/// Event triggered when a user is updated.
/// </summary>
/// <param name="userId">The user ID.</param>
/// <param name="firstName">The first name of the user.</param>
/// <param name="lastName">The last name of the user.</param>
public class UserUpdatedEvent(string userId, string firstName, string lastName) : IEvent
{
    /// <summary>
    /// Gets the user ID.
    /// </summary>
    public string UserId { get; } = userId;

    /// <summary>
    /// Gets the first name of the user.
    /// </summary>
    public string FirstName { get; } = firstName;

    /// <summary>
    /// Gets the last name of the user.
    /// </summary>
    public string LastName { get; } = lastName;
}

/// <summary>
/// Event triggered when a user is validated.
/// </summary>
/// <param name="userId">The user ID.</param>
/// <param name="firstName">The first name of the user.</param>
/// <param name="lastName">The last name of the user.</param>
public class UserValidationEvent(string userId, string firstName, string lastName) : IEvent
{
    /// <summary>
    /// Gets the user ID.
    /// </summary>
    public string UserId { get; } = userId;

    /// <summary>
    /// Gets the first name of the user.
    /// </summary>
    public string FirstName { get; } = firstName;

    /// <summary>
    /// Gets the last name of the user.
    /// </summary>
    public string LastName { get; } = lastName;
}


/// <summary>
/// Event triggered when a user's role is updated.
/// </summary>
/// <param name="userId">The user ID.</param>
/// <param name="firstName">The first name of the user.</param>
/// <param name="lastName">The last name of the user.</param>
/// <param name="oldRoles">The existing roles of the user.</param>
/// <param name="newRoles">The new roles of the user.</param>
public class UserRoleUpdatedEvent(string userId, string firstName, string lastName, List<RolesEnum> oldRoles, List<RolesEnum> newRoles) : IEvent
{
    /// <summary>
    /// Gets the user ID.
    /// </summary>
    public string UserId { get; } = userId;

    /// <summary>
    /// Gets the first name of the user.
    /// </summary>
    public string FirstName { get; } = firstName;

    /// <summary>
    /// Gets the last name of the user.
    /// </summary>
    public string LastName { get; } = lastName;

    /// <summary>
    /// Gets the existing roles of the user.
    /// </summary>
    public List<RolesEnum> OldRoles { get; } = oldRoles;

    /// <summary>
    /// Gets the new roles of the user.
    /// </summary>
    public List<RolesEnum> NewRoles { get; } = newRoles;
}