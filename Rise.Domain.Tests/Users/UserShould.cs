using Rise.Domain.Users;
using Rise.Shared.Enums;
using Shouldly;

namespace Rise.Domain.Tests.Users;

public class UserShould
{
    [Fact]
    public void ShouldCreateCorrectUser()
    {
        User user = new User("1","Fedor", "Danilov", "fp@email.com",  DateTime.Today, new Address("Afrikalaan", "1"), "000");
        
        user.FirstName.ShouldBe("Fedor");
        user.LastName.ShouldBe("Danilov");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("")]
    public void ShouldThrowIncorrectFirstname(string? firstname)
    {
        Action action = () =>
        {
            new User("1",firstname, "Danilov", "fp@email.com",  DateTime.Today, new Address("Afrikalaan", "1"), "000");
        };

        action.ShouldThrow<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("")]
    public void ShouldThrowIncorrectLastname(string? lastname)
    {
        Action action = () =>
        {
            new User("1","Fedor", lastname, "fp@email.com",  DateTime.Today, new Address("Afrikalaan", "1"), "000");
        };

        action.ShouldThrow<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("")]
    [InlineData("invalid-email")]
    public void ShouldThrowIncorrectEmail(string? email)
    {
        Action action = () =>
        {
            new User("1","Fedor", "Danilov", email,  DateTime.Today, new Address("Afrikalaan", "1"), "000");
        };

        action.ShouldThrow<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("")]
    public void ShouldThrowIncorrectPhonenumber(string? phonenumber)
    {
        Action action = () =>
        {
            new User(",","Fedor", "Danilov", "fp@email.com", DateTime.Today, new Address("Afrikalaan", "1"), phonenumber);
        };

        action.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void ShouldAssignCorrectAddressToUser()
    {
        User user = new User("1","Fedor", "Danilov", "fp@email.com", DateTime.Today, new Address("Afrikalaan", "1"), "000");

        user.Address = new Address("Dok Noord", "20", "A");
        
        user.Address.Street.ShouldBe("Dok Noord");
        user.Address.HouseNumber.ShouldBe("20");
        user.Address.Bus.ShouldBe("A");
    }

    [Fact]
    public void ShouldAssignCorrectRoleToUser()
    {
        User user = new User("1","Fedor", "Danilov", "fp@email.com", DateTime.Today, new Address("Afrikalaan", "1"), "000");

        user.AddRole(new Role());
        
        user.Roles.ShouldNotBeEmpty();
    }

    [Fact]
    public void HasRole_UserHasGivenRole_ReturnsTrue()
    {
        // setup scenario
        User user = new User("1","Fedor", "Danilov", "fp@email.com", DateTime.Today, new Address("Afrikalaan", "1"), "000");
        Role role = new Role(RolesEnum.Admin);
        Role checkRole = new Role(RolesEnum.Admin);
        user.AddRole(role);
        
        Assert.True(user.HasRole(checkRole));
    }

    [Fact]
    public void HasRole_UserDoesNotHaveGivenRole_ReturnsFalse()
    {
        // setup scenario
        User user = new User("1","Fedor", "Danilov", "fp@email.com", DateTime.Today, new Address("Afrikalaan", "1"), "000");
        Role role = new Role(RolesEnum.Admin);
        Role checkRole = new Role(RolesEnum.User);
        user.AddRole(role);
        
        Assert.False(user.HasRole(checkRole));
    }

    [Fact]
    public void HasRole_UserHasGivenRoleInListWithMultiple_ReturnsTrue()
    {
        // setup scenario
        User user = new User("1","Fedor", "Danilov", "fp@email.com", DateTime.Today, new Address("Afrikalaan", "1"), "000");
        Role role1 = new Role(RolesEnum.Admin);
        Role role2 = new Role(RolesEnum.User);
        Role role3 = new Role(RolesEnum.BUUTAgent);
        Role checkRole = new Role(RolesEnum.User);
        user.AddRole(role1);
        user.AddRole(role2);
        user.AddRole(role3);
        
        Assert.True(user.HasRole(checkRole));
    }

    [Fact]
    public void HasRole_UserDoesNotHaveGivenRoleInListWithMultiple_ReturnsFalse()
    {
        // setup scenario
        User user = new User("1","Fedor", "Danilov", "fp@email.com", DateTime.Today, new Address("Afrikalaan", "1"), "000");
        
        // Roles for the user
        Role role1 = new Role(RolesEnum.Admin);
        Role role2 = new Role(RolesEnum.Pending);
        Role role3 = new Role(RolesEnum.BUUTAgent);

        // Role user does not have
        Role checkRole = new Role(RolesEnum.User);

        // Give user roles
        user.AddRole(role1);
        user.AddRole(role2);
        user.AddRole(role3);
        
        // Check
        Assert.False(user.HasRole(checkRole));
    }
}
