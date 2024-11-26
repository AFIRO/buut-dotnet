namespace Rise.Shared.Bookings;

public class BoatDto
{
    public class NewBoat
    {
        public string? name { get; set; }= default!;
    }

    public class ViewBoat
    {
        public string name { get; set; }= default!;
        public int countBookings { get; set; } = default!;
        public List<string> listComments = default!;
    }
}