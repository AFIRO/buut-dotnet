using Rise.Domain.Bookings;
using Rise.Domain.Users;
using Rise.Shared.Enums;
using Shouldly;

namespace Rise.Domain.Tests.Bookings;

public class BatteryShould
{
    [Fact]
    public void ShouldCreateCorrectBattery()
    {
        string name = "Battery";
        int countBookings = 1;
        List<string> listComments = new List<string>();
        listComments.Add("comment1");
        listComments.Add("comment2");
        Battery battery = new Battery(name, countBookings, listComments);
        battery.Name.ShouldBe(name);
        battery.CountBookings.ShouldBe(countBookings);
        battery.ListComments.ShouldBe(listComments);
    }

    [Fact]
    public void Constructor_WithHolderAndGodparent_ShouldCreateCorrectBattery()
    {
        // generate all the data
        string name = "Battery";
        int countBookings = 1;
        List<string> listComments = new List<string> { "comment1", "comment2" };
        User godparent = new User
        ("test", 
        "firstName", 
        "lastName", 
        "test@email.com", 
        DateTime.Today.AddYears(-21),
        new Address("Afrikalaan", "1"), 
        "012345678"
        );
        godparent.AddRole(new Role(RolesEnum.BUUTAgent));
        User holder = new User
        ("test2", 
        "firstNameHolder", 
        "lastNameHolder", 
        "test.holder@email.com", 
        DateTime.Today.AddYears(-21),
        new Address("Afrikalaan", "2"), 
        "023456789"
        );

        //Build the battery
        Battery battery = new Battery(name, godparent, holder, countBookings, listComments);

        // check for correctness battery
        battery.Name.ShouldBe(name);
        battery.CountBookings.ShouldBe(countBookings);
        battery.ListComments.ShouldBe(listComments);
        battery.BatteryBuutAgent.ShouldBe(godparent);
        battery.CurrentUser.ShouldBe(holder);
    }

    [Fact]
    public void Constructor_WithGodparent_ShouldCreateCorrectBattery()
    {
        // generate all the data
        string name = "Battery";
        int countBookings = 1;
        List<string> listComments = new List<string> { "comment1", "comment2" };
        User godparent = new User
        ("test", 
        "firstName", 
        "lastName", 
        "test@email.com", 
        DateTime.Today.AddYears(-21),
        new Address("Afrikalaan", "1"), 
        "012345678"
        );
        godparent.AddRole(new Role(RolesEnum.BUUTAgent));

        //Build the battery
        Battery battery = new Battery(name, godparent, countBookings, listComments);

        // check for correctness battery
        battery.Name.ShouldBe(name);
        battery.CountBookings.ShouldBe(countBookings);
        battery.ListComments.ShouldBe(listComments);
        battery.BatteryBuutAgent.ShouldBe(godparent);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("    ")]
    [InlineData("")]
    public void ShouldThrowInvalidName(string name)
    {
        int countBookings = 1;
        List<string> listComments = new List<string>();
        listComments.Add("comment1");
        listComments.Add("comment2");
        Action action = () => { new Battery(name, null, countBookings, listComments); };

        action.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void ShouldThrowInvalidCountBookings()
    {
        string name = "Battery";
        int countBookings = -1;
        List<string> listComments = new List<string>();
        listComments.Add("comment1");
        listComments.Add("comment2");
        Action action = () => { new Battery(name, null, countBookings, listComments); };

        action.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void ShouldThrowInvalidListComments()
    {
        string name = "Battery";
        int countBookings = 1;
        List<string> listComments = null;
        Action action = () => { new Battery(name, null, countBookings, listComments); };

        action.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void ShouldAddBooking()
    {
        Battery battery = new Battery("battery");
        battery.AddBooking();
        battery.CountBookings.ShouldBe(1);
    }

    [Fact]
    public void ShouldAddComment()
    {
        Battery battery = new Battery("battery");
        battery.AddComment("String");
        battery.ListComments.Count.ShouldBe(1);
    }

    [Fact]
    public void ShouldAddCurrentUser()
    {
        Battery battery = new Battery("battery");
        battery.ChangeCurrentUser(new User("1", "f", "d", "fd@mail.com", DateTime.Now, new Address(StreetEnumExtensions.GetStreetName(StreetEnum.DOKNOORD), "3"), "12346579"));
        battery.CurrentUser.ShouldNotBeNull();
    }

    [Fact]
    public void ShouldAddBuutAgent()
    {
        Battery battery = new Battery("battery");
        User godparent = new User
        ("test", 
        "firstName", 
        "lastName", 
        "test@email.com", 
        DateTime.Today.AddYears(-21),
        new Address("Afrikalaan", "1"), 
        "012345678"
        );
        godparent.AddRole(new Role(RolesEnum.BUUTAgent));
        
        battery.ChangeBuutAgent(godparent);
        battery.BatteryBuutAgent.ShouldNotBeNull();
    }

    [Fact]
    public void ShouldThrowInvalidComment()
    {
        Battery battery = new Battery("battery");
        Action action = () => { battery.AddComment(null); };

        action.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void SetBatteryBuutAgent_giveUserWithCorrectRoleWhenBatteryHasNoGodparent_assignsUser()
    {
        User godparent = new User
        ("test", 
        "firstName", 
        "lastName", 
        "test@email.com", 
        DateTime.Today.AddYears(-21),
        new Address("Afrikalaan", "1"), 
        "012345678"
        );
        godparent.AddRole(new Role(RolesEnum.BUUTAgent));

        Battery battery = new Battery("battery");

        battery.SetBatteryBuutAgent(godparent);

        Assert.Equal(godparent, battery.BatteryBuutAgent);
    }

    [Fact]
    public void SetBatteryBuutAgent_giveUserWithCorrectRoleWhenBatteryHasGodparent_assignsUser()
    {
        // make new and old godparent and give correct role
        User firstGodparent = new User
        ("test", 
        "firstName", 
        "lastName", 
        "test@email.com", 
        DateTime.Today.AddYears(-21),
        new Address("Afrikalaan", "1"), 
        "012345678"
        );
        firstGodparent.AddRole(new Role(RolesEnum.BUUTAgent));

        User newGodparent = new User
        ("test", 
        "firstName", 
        "lastName", 
        "test@email.com", 
        DateTime.Today.AddYears(-21),
        new Address("Afrikalaan", "1"), 
        "012345678"
        );
        newGodparent.AddRole(new Role(RolesEnum.BUUTAgent));


        // make battery and give godparent
        Battery battery = new Battery("battery");
        battery.SetBatteryBuutAgent(firstGodparent);

        // assert correct setup
        Assert.Equal(firstGodparent, battery.BatteryBuutAgent);


        // change godparent and assert change
        battery.SetBatteryBuutAgent(newGodparent);
        Assert.Equal(newGodparent, battery.BatteryBuutAgent);
    }

    [Fact]
    public void SetBatteryBuutAgent_giveUserWithInCorrectRoleWhenBatteryHasGodparent_assignsUser()
    {
        // make new and old godparent and give correct role for the tests
        User firstGodparent = new User
        ("test", 
        "firstName", 
        "lastName", 
        "test@email.com", 
        DateTime.Today.AddYears(-21),
        new Address("Afrikalaan", "1"), 
        "012345678"
        );
        firstGodparent.AddRole(new Role(RolesEnum.BUUTAgent));

        User newGodparent = new User
        ("test", 
        "firstName", 
        "lastName", 
        "test@email.com", 
        DateTime.Today.AddYears(-21),
        new Address("Afrikalaan", "1"), 
        "012345678"
        );
        newGodparent.AddRole(new Role(RolesEnum.User));


        // make battery and give godparent
        Battery battery = new Battery("battery");
        battery.SetBatteryBuutAgent(firstGodparent);

        // assert correct setup
        Assert.Equal(firstGodparent, battery.BatteryBuutAgent);


        // try to change godparent and assert change
        Assert.Throws<InvalidOperationException>(() => battery.SetBatteryBuutAgent(newGodparent));
        Assert.Equal(firstGodparent, battery.BatteryBuutAgent);
    }

    [Fact]
    public void SetBatteryBuutAgent_giveUserWithInCorrectRole_throwsInvalidOperationException()
    {
        User godparent = new User
        ("test", 
        "firstName", 
        "lastName", 
        "test@email.com", 
        DateTime.Today.AddYears(-21),
        new Address("Afrikalaan", "1"), 
        "012345678"
        );
        godparent.AddRole(new Role(RolesEnum.User));

        Battery battery = new Battery("battery");

        Assert.Throws<InvalidOperationException>(() => battery.SetBatteryBuutAgent(godparent));
        
        Assert.Null(battery.BatteryBuutAgent);
    }

    [Fact]
    public void SetBatteryBuutAgent_giveNull_removesCurrentGodparent()
    {
        // setup user + make buutagent + make godparent to battery
        User godparent = new User
        ("test", 
        "firstName", 
        "lastName", 
        "test@email.com", 
        DateTime.Today.AddYears(-21),
        new Address("Afrikalaan", "1"), 
        "012345678"
        );
        godparent.AddRole(new Role(RolesEnum.BUUTAgent));
        Battery battery = new Battery("testBattery", godparent, 0, new List<string>());

        // check that battery has a godparent
        Assert.Equal(godparent, battery.BatteryBuutAgent);

        // set the godparent to null (aka remove)
        battery.SetBatteryBuutAgent(null);
        
        // assert the battery does not have a godparent
        Assert.Null(battery.BatteryBuutAgent);
    }

    [Fact]
    public void RemoveBatteryBuutAgent_removesCurrentGodparent()
    {
        // setup user + make buutagent + make godparent to battery
        User godparent = new User
        ("test", 
        "firstName", 
        "lastName", 
        "test@email.com", 
        DateTime.Today.AddYears(-21),
        new Address("Afrikalaan", "1"), 
        "012345678"
        );
        godparent.AddRole(new Role(RolesEnum.BUUTAgent));
        Battery battery = new Battery("testBattery", godparent, 0, new List<string>());

        // check that battery has a godparent
        Assert.Equal(godparent, battery.BatteryBuutAgent);
        
        // set the godparent to null (aka remove)
        battery.RemoveBatteryBuutAgent();
        
        // assert the battery does not have a godparent
        Assert.Null(battery.BatteryBuutAgent);
    }
    




    [Fact]
    public void SetHolder_giveUserUser_assignsUser()
    {
        User holder = new User
        ("test", 
        "firstName", 
        "lastName", 
        "test@email.com", 
        DateTime.Today.AddYears(-21),
        new Address("Afrikalaan", "1"), 
        "012345678"
        );
        holder.AddRole(new Role(RolesEnum.User));

        Battery battery = new Battery("battery");

        battery.CurrentUser = holder;

        Assert.Equal(holder, battery.CurrentUser);
    }

    [Fact]
    public void SetHolder_giveAdminUser_assignsUser()
    {
        User holder = new User
        ("test", 
        "firstName", 
        "lastName", 
        "test@email.com", 
        DateTime.Today.AddYears(-21),
        new Address("Afrikalaan", "1"), 
        "012345678"
        );
        holder.AddRole(new Role(RolesEnum.User));

        Battery battery = new Battery("battery");

        battery.CurrentUser = holder;

        Assert.Equal(holder, battery.CurrentUser);
    }
    [Fact]
    public void SetHolder_giveBUUTAgentUser_assignsUser()
    {
        User holder = new User
        ("test", 
        "firstName", 
        "lastName", 
        "test@email.com", 
        DateTime.Today.AddYears(-21),
        new Address("Afrikalaan", "1"), 
        "012345678"
        );
        holder.AddRole(new Role(RolesEnum.BUUTAgent));

        Battery battery = new Battery("battery");

        battery.CurrentUser = holder;

        Assert.Equal(holder, battery.CurrentUser);
    }
    [Fact]
    public void SetHolder_giveUserWithoutRole_assignsUser()
    {
        User holder = new User
        ("test", 
        "firstName", 
        "lastName", 
        "test@email.com", 
        DateTime.Today.AddYears(-21),
        new Address("Afrikalaan", "1"), 
        "012345678"
        );

        Battery battery = new Battery("battery");

        battery.CurrentUser = holder;

        Assert.Equal(holder, battery.CurrentUser);
    }

    [Fact]
    public void SetHolder_giveNull_throwsException()
    {
        Battery battery = new Battery("battery");

        Assert.Throws<InvalidOperationException>(() => battery.CurrentUser = null);

        
    }

    
}