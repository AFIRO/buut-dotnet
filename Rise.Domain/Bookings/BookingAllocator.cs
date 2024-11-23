namespace Rise.Domain.Bookings;

public class BookingAllocator
{
    private List<Battery> getYesterdaysBatteries(List<Booking> bookings, DateTime yesterday)
    {
        return bookings.Where(x => x.BookingDate.Date == yesterday.Date).Select(x => x.Battery).ToList();
    }

    private List<Battery> getAvailableBatteries(List<Booking> bookings, List<Battery> batteries, DateTime today)
    {
        var yesterdaysBatteries = getYesterdaysBatteries(bookings, today.AddDays(-1));
        foreach (var yBattery in yesterdaysBatteries)
        {
            batteries.Remove(yBattery);
        }
        //Return the batteries that are available, lowest usage first
        return batteries.OrderBy(x => x.CountBookings).ToList();
    }

    private List<Booking> getTodaysBookings(List<Booking> bookings, DateTime today)
    {
        //Return all the bookings that are booked for today, first of the day first
        return bookings.Where(x => x.BookingDate.Date == today.Date)
            .OrderBy(x => x.BookingDate.Hour).ToList();
    }

    public void assignBatteriesBoats(List<Booking> bookings, List<Battery> batteries, List<Boat> boats, DateTime today)
    {
        var availableBatteries = getAvailableBatteries(bookings, batteries, today.Date);
        var availableBoats = boats.OrderBy(x => x.CountBookings).ToList();
        var todaysBookings = getTodaysBookings(bookings, today.Date);

        foreach (var (booking, index) in todaysBookings.Select((value, index) => (value, index)))
        {
            booking.AddBattery(availableBatteries[index]);
            booking.AddBoat(availableBoats[index]);
        }
    } 
}