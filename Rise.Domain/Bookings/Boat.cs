namespace Rise.Domain.Bookings;

public class Boat : Entity
{
    private string _id = Guid.NewGuid().ToString();
    private string _name = default!;
    private int _countBookings = default!;
    private List<string> _listComments = new List<string>();

    /// <summary>
    ///Private constructor for Entity Framework Core
    /// </summary>
    private Boat()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Boat"/> class with the specified details.
    /// </summary>
    /// <param name="name">The name of the boat.</param>
    public Boat(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Boat"/> class with the specified details.
    /// </summary>
    /// <param name="name">The name of the boat.</param>
    /// <param name="countBookings">The amount of bookings that has been done.</param>
    /// <param name="listComments">A list with comments.</param>
    public Boat(string name, int countBookings, List<string> listComments) : this(name)
    {
        CountBookings = countBookings;
        ListComments = listComments;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Boat"/> class with the specified details.
    /// </summary>
    /// <param name="boatId">The id of the boat</param>
    /// <param name="name">The name of the boat.</param>
    /// <param name="countBookings">The amount of bookings that has been done.</param>
    /// <param name="listComments">A list with comments.</param>
    public Boat(string boatId, string name, int countBookings, List<string> listComments) : this(name)
    {
        Id = boatId;
        CountBookings = countBookings;
        ListComments = listComments;
    }

    #region Properties

    public string Id
    {
        get => _id;
        set => _id = Guard.Against.NullOrWhiteSpace(value, nameof(Id));
    }    
    /// <summary>
    /// Gets or sets the name of the boat.
    /// </summary>
    public string Name
    {
        get => _name;
        set => _name = Guard.Against.NullOrWhiteSpace(value, nameof(_name));
    }

    /// <summary>
    /// Gets or sets the amount of bookings that have been booked on this boat.
    /// </summary>
    public int CountBookings
    {
        get => _countBookings;
        set => _countBookings = Guard.Against.Negative(value, nameof(CountBookings));
    }

    /// <summary>
    /// Gets or sets the list with comments.
    /// </summary>
    public List<string> ListComments
    {
        get => _listComments;
        set => _listComments = value ?? throw new ArgumentException("ListComments cannot be null", nameof(ListComments));
    }

    #endregion

    #region Methods

    /// <summary>
    /// Increases booking counter by 1.
    /// </summary>
    public void AddBooking()
    {
        _countBookings += 1;
    }

    /// <summary>
    /// Adds a comment to the list.
    /// </summary>
    /// <param name="comment">The comment to add.</param>
    public void AddComment(string comment)
    {
        Guard.Against.NullOrWhiteSpace(comment, nameof(comment));
        _listComments.Add(comment);
    }

    #endregion
}