namespace Rise.Domain.Bookings;

/// <summary>
/// Class responsible for allocating batteries and boats to bookings.
/// </summary>
public class BookingAllocator
{
    /// <summary>
    /// Retrieves the batteries used in yesterday's bookings.
    /// </summary>
    /// <param name="bookings">The list of bookings.</param>
    /// <param name="yesterday">The date representing yesterday.</param>
    /// <returns>A list of batteries used in yesterday's bookings.</returns>
    private List<Battery> getYesterdaysBatteries(List<Booking> bookings, DateTime yesterday)
    {
        return bookings.Where(x => x.BookingDate.Date == yesterday.Date).Select(x => x.Battery).ToList();
    }

    /// <summary>
    /// Retrieves the batteries available for today's bookings.
    /// </summary>
    /// <param name="bookings">The list of bookings.</param>
    /// <param name="batteries">The list of all batteries.</param>
    /// <param name="today">The date representing today.</param>
    /// <returns>A list of available batteries, ordered by lowest usage first.</returns>
    private List<Battery> getAvailableBatteries(List<Booking> bookings, List<Battery> batteries, DateTime today)
    {
        var yesterdaysBatteries = getYesterdaysBatteries(bookings, today.AddDays(-1));
        foreach (var yBattery in yesterdaysBatteries)
        {
            batteries.Remove(yBattery);
        }
        // Return the batteries that are available, lowest usage first
        return batteries.OrderBy(x => x.CountBookings).ToList();
    }

    /// <summary>
    /// Retrieves the bookings for today.
    /// </summary>
    /// <param name="bookings">The list of bookings.</param>
    /// <param name="today">The date representing today.</param>
    /// <returns>A list of today's bookings, ordered by the hour of the booking.</returns>
    private List<Booking> getTodaysBookings(List<Booking> bookings, DateTime today)
    {
        // Return all the bookings that are booked for today, first of the day first
        return bookings.Where(x => x.BookingDate.Date == today.Date)
            .OrderBy(x => x.BookingDate.Hour).ToList();
    }

    /// <summary>
    /// Assigns batteries and boats to today's bookings.
    /// </summary>
    /// <param name="bookings">The list of bookings.</param>
    /// <param name="batteries">The list of all batteries.</param>
    /// <param name="boats">The list of all boats.</param>
    /// <param name="today">The date representing today.</param>
    public void assignBatteriesBoats(List<Booking> bookings, List<Battery> batteries, List<Boat> boats, DateTime today)
    {
        var availableBatteries = getAvailableBatteries(bookings, batteries, today.Date);
        var availableBoats = boats.OrderBy(x => x.CountBookings).ToList();
        var todaysBookings = getTodaysBookings(bookings, today.Date);
        
        var cntAvailableBatteries = availableBatteries.Count;
        var cntAvailableBoats = availableBoats.Count;

        foreach (var (booking, index) in todaysBookings.Select((value, index) => (value, index)))
        {
            if (index < cntAvailableBoats)
            {
                booking.AddBoat(availableBoats[index]);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(boats), "Not enough boats available for today's bookings.");
            }

            if (index < cntAvailableBatteries)
            {
                booking.AddBattery(availableBatteries[index]);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(batteries), "Not enough batteries available for today's bookings.");
            }
        }
    } 
}