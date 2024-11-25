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
}
