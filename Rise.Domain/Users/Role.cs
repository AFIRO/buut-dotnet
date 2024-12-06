namespace Rise.Domain.Users;
using Rise.Shared.Enums;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Represents a role assigned to a user in the system.
/// </summary>
public class Role : Entity
{
    private RolesEnum _name;

    /// <summary>
    /// Gets or sets the name of the role.
    /// </summary>
    /// <remarks>
    /// The role name is based on the <see cref="RolesEnum"/> enumeration.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned role name is not valid according to the <see cref="RolesEnum"/>.
    /// </exception>
    [Column(TypeName = "nvarchar(50)")]
    public RolesEnum Name
    {
        get => _name;
        set => _name = Guard.Against.EnumOutOfRange(value);
    }

    /// <summary>
    /// Private constructor for Entity Framework Core.
    /// </summary>
    private Role()
    {
    }
    
    // public string UserId
    // {
    //     get => _userId;
    //     set => _userId = Guard.Against.Null(value);
    // }

    /// <summary>
    /// Initializes a new instance of the <see cref="Role"/> class with a specified role name.
    /// </summary>
    /// <param name="name">The role name to assign. Defaults to <see cref="RolesEnum.User"/>.</param>
    public Role(RolesEnum name = RolesEnum.User)
    {
        Name = name;
    }

    public List<User> Users { get; set; } = [];

    #region Equality Overrides

    /// <summary>
    /// Checks if the current role is equal to another role.
    /// </summary>
    /// <param name="other">The role to compare with.</param>
    /// <returns>True if the roles are equal, false otherwise.</returns>
    public bool Equals(Role? other)
    {
        if (other is null)
            return false;

        // Check if the RolesEnum value matches
        return Name == other.Name;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current role.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is Role otherRole)
            return Equals(otherRole);

        return false;
    }

    /// <summary>
    /// Serves as a hash function for the role.
    /// </summary>
    public override int GetHashCode()
    {
        // Use the RolesEnum value for hash code generation
        return Name.GetHashCode();
    }

    #endregion

}
