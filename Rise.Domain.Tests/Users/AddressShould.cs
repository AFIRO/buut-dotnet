using Rise.Domain.Users;
using Shouldly;

namespace Rise.Domain.Tests.Users;

public class AddressShould
{
    [Fact]
    public void ShouldAssignCorrectStreet()
    {
        Address address = new Address("Afrikalaan", "1");
        
        address.Street.ShouldBe("Afrikalaan");
    }

    [Fact]
    public void ShouldThrowIncorrectStreet()
    {
        Action action = () =>
        {
            new Address("Waregemsestraat", "1");
        };

        action.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void ShouldAssignCorrectHouseNumber()
    {
        Address address = new Address("Afrikalaan", "1");
        
        address.HouseNumber.ShouldBe("1");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("a")]
    [InlineData("10+")]

    public void ShouldThrowIncorrectHouseNumber(string houseNumber)
    {
        Action act = () =>
        {
            Address address = new Address("Afrikalaan", houseNumber);
        };

        act.ShouldThrow<ArgumentOutOfRangeException>();
    }

}
