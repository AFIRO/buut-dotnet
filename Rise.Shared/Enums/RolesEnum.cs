using System.Runtime.Serialization;

namespace Rise.Shared.Enums;

public enum RolesEnum
{
    [EnumMember(Value = "Admin")]
    Admin,
    [EnumMember(Value = "User")]
    User,
    [EnumMember(Value = "BUUTAgent")]
    BUUTAgent,
    [EnumMember(Value = "Pending")]
    Pending,
}
public static class RolesEnumExtensions
{


    /// <summary>
    /// Converts a string to a RolesEnum.
    /// </summary>
    /// <param name="roleName">The string representation of the enum.</param>
    /// <returns>The corresponding RolesEnum value.</returns>
    public static RolesEnum FromString(string roleName)
    {
        if (Enum.TryParse(roleName, true, out RolesEnum roleEnum))
        {
            return roleEnum;
        }

        throw new ArgumentException($"Invalid role name: {roleName}");
    }

    /// <summary>
    /// Converts a RolesEnum to its string representation.
    /// </summary>
    /// <param name="roleEnum">The RolesEnum value.</param>
    /// <returns>The string representation of the enum.</returns>
    public static string ToStringValue(this RolesEnum roleEnum)
    {
        return roleEnum.ToString();
    }
}