namespace Rise.Shared.Boats;

public class BoatDto
{
    public class NewBoat
    {
        public string? name { get; set; } = default!;
    }

    public class ViewBoat
    {
        public string boatId { get; set; } = default!;
        public string name { get; set; } = default!;
        public int countBookings { get; set; } = default!;
        public List<string> listComments = default!;
    }

    public class UpdateBoat
    {
        public string id { get; set; } = default!;
        public string? name { get; set; } = default!;
    }
}