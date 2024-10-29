using Rise.Domain.Bookings;
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
        Action action = () => { new Battery(name, countBookings, listComments); };

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
        Action action = () => { new Battery(name, countBookings, listComments); };

        action.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void ShouldThrowInvalidListComments()
    {
        string name = "Battery";
        int countBookings = 1;
        List<string> listComments = null;
        Action action = () => { new Battery(name, countBookings, listComments); };

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
    public void ShouldThrowInvalidComment()
    {
        Battery battery = new Battery("battery");
        Action action = () => { battery.AddComment(null); };

        action.ShouldThrow<ArgumentException>();
    }
    
}