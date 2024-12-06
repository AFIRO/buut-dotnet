using Rise.Domain.Common;
using Rise.Domain.Users;
using Rise.Shared.Enums;
using Shouldly;

namespace Rise.Domain.Tests.Users;

public class RoleShould
{
    private enum RolesTestEnum
    {
        Test
    }
    
    [Fact]
    public void ShouldAssignCorrectRole()
    {
        Role role = new Role();
        role.Name = RolesEnum.BUUTAgent;
        
        role.Name.ShouldBe(RolesEnum.BUUTAgent);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenRolesAreEqual()
    {
        // Arrange
        var role1 = new Role(RolesEnum.Admin);
        var role2 = new Role(RolesEnum.Admin);

        // Act
        bool result = role1.Equals(role2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenRolesAreNotEqual()
    {
        // Arrange
        var role1 = new Role(RolesEnum.Admin);
        var role2 = new Role(RolesEnum.User);

        // Act
        bool result = role1.Equals(role2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparedWithNull()
    {
        // Arrange
        var role = new Role(RolesEnum.Admin);

        // Act
        bool result = role.Equals(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void EqualsObject_ShouldReturnTrue_WhenRolesAreEqual()
    {
        // Arrange
        var role1 = new Role(RolesEnum.Admin);
        object role2 = new Role(RolesEnum.Admin);

        // Act
        bool result = role1.Equals(role2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void EqualsObject_ShouldReturnFalse_WhenRolesAreNotEqual()
    {
        // Arrange
        var role1 = new Role(RolesEnum.Admin);
        object role2 = new Role(RolesEnum.User);

        // Act
        bool result = role1.Equals(role2);

        // Assert
        Assert.False(result);
    }
    [Fact]
    public void EqualsObject_ShouldReturnFalse_WhenNonRoleObject()
    {
        // Arrange
        var role1 = new Role(RolesEnum.Admin);
        object role2 = "No Role";

        // Act
        bool result = role1.Equals(role2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void EqualsObject_ShouldReturnFalse_WhenNullRoleObject()
    {
        // Arrange
        var role1 = new Role(RolesEnum.Admin);
        object role2 = null;

        // Act
        bool result = role1.Equals(role2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetHashCode_ShouldReturnSameHashCode_ForEqualRoles()
    {
        // Arrange
        var role1 = new Role(RolesEnum.Admin);
        var role2 = new Role(RolesEnum.Admin);

        // Act
        int hashCode1 = role1.GetHashCode();
        int hashCode2 = role2.GetHashCode();

        // Assert
        Assert.Equal(hashCode1, hashCode2);
    }

    [Fact]
    public void GetHashCode_ShouldReturnDifferentHashCode_ForDifferentRoles()
    {
        // Arrange
        var role1 = new Role(RolesEnum.Admin);
        var role2 = new Role(RolesEnum.User);

        // Act
        int hashCode1 = role1.GetHashCode();
        int hashCode2 = role2.GetHashCode();

        // Assert
        Assert.NotEqual(hashCode1, hashCode2);
    }
}
