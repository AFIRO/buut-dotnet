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
}
