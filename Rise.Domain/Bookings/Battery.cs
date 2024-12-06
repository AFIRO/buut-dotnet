using System.Security.Cryptography;
using Rise.Domain.Users;
using Rise.Shared.Bookings;
using Rise.Shared.Enums;

using Rise.Domain.Users;

namespace Rise.Domain.Bookings;

/// <summary>
/// Represents a battery entity with associated properties and methods.
/// </summary>
public class Battery : Entity
{
    private const RolesEnum REQUIRED_ROLE_BATTERYBUUTAGENT = RolesEnum.BUUTAgent;

    private string _id = Guid.NewGuid().ToString();
    private User? _batteryBuutAgent = null;
    private User? _currentUser = null;
    private string _name = default!;
    private int _countBookings = default!;
    private List<string> _listComments = new List<string>();
    
    /// <summary>
    ///Private constructor for Entity Framework Core
    /// </summary>
    private Battery()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Battery"/> class with the specified details.
    /// </summary>
    /// <param name="name">The name of the battery.</param>
    public Battery(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Battery"/> class with the specified details.
    /// </summary>
    /// <param name="name">The name of the battery.</param>
    /// <param name="countBookings">The amount of bookings that has been done.</param>
    /// <param name="listComments">A list with comments.</param>
    /// <param name="batteryBuutAgent">The batteryBuutAgent for the battery.</param>
    public Battery(string name, User batteryBuutAgent, int countBookings, List<string> listComments) : this(name)
    {
        CountBookings = countBookings;
        ListComments = listComments;
        AssignBatteryBuutAgent(batteryBuutAgent);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Battery"/> class with the specified details.
    /// </summary>
    /// <param name="name">The name of the battery.</param>
    /// <param name="countBookings">The amount of bookings that has been done.</param>
    /// <param name="listComments">A list with comments.</param>
    public Battery(string name, int countBookings, List<string> listComments) : this(name)
    {
        CountBookings = countBookings;
        ListComments = listComments;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Battery"/> class with the specified details.
    /// </summary>
    /// <param name="name">The name of the battery.</param>
    /// <param name="countBookings">The amount of bookings that has been done.</param>
    /// <param name="listComments">A list with comments.</param>
    /// <param name="batteryBuutAgent">The batteryBuutAgent for the battery.</param>
    /// <param name="currentUser">The user who has the battery in their possession.</param>
    public Battery(string name, User batteryBuutAgent, User currentUser, int countBookings, List<string> listComments) : this(name)
    {
        CountBookings = countBookings;
        ListComments = listComments;
        AssignBatteryBuutAgent(batteryBuutAgent);
        CurrentUser = currentUser;
    }
    
    #region Properties
    /// <summary>
    /// Gets or sets the batteryBuutAgent for the current object.
    /// </summary>
    /// <value>
    /// The <see cref="User"/> who is assigned as the batteryBuutAgent.
    /// </value>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the user does not have the required role to be assigned as the batteryBuutAgent.
    /// </exception>
    public User? BatteryBuutAgent
    {
        get => _batteryBuutAgent;
        private set => AssignBatteryBuutAgent(value);
    }

    /// <summary>
    /// Gets or sets the currentUser of the battery. Must not be null after creation.
    /// </summary>
    public User? CurrentUser
    {
        get => _currentUser;
        set
        {
            if (Equals(_currentUser, null) && Equals(value, null))
            {
                throw new InvalidOperationException("currentUser cannot be null after creation.");
            }
            _currentUser = Guard.Against.Null(value);
        }
    }

    
    /// <summary>
    /// Gets or sets the id of the battery.
    /// </summary>
    public string Id
    {
        get => _id;
        set => _id = Guard.Against.NullOrWhiteSpace(value, nameof(Id));
    }    
    /// <summary>
    /// Gets or sets the name of the battery.
    /// </summary>
    public string Name
    {
        get => _name;
        set => _name = Guard.Against.NullOrWhiteSpace(value, nameof(_name));
    }

    /// <summary>
    /// Gets or sets the amount of bookings that have been booked on this battery.
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

    /// <summary>
    /// Sets the batteryBuutAgent for the battery.
    /// </summary>
    /// <param name="batteryBuutAgent">The batteryBuutAgent you want to assign to the battery.</param>
    public void SetBatteryBuutAgent(User? batteryBuutAgent)
    {
        AssignBatteryBuutAgent(batteryBuutAgent);
    }

    /// <summary>
    /// Remove the batteryBuutAgent from to the battery.
    /// </summary>
    public void RemoveBatteryBuutAgent()
    {
        AssignBatteryBuutAgent(null);
    }

    /// <summary>
    /// Assigns the batteryBuutAgent to the battery.
    /// </summary>
    /// <value>
    /// The <see cref="User"/> who is assigned as the batteryBuutAgent.
    /// </value>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the user does not have the required role to be assigned as the batteryBuutAgent.
    /// </exception>
    private void AssignBatteryBuutAgent(User batteryBuutAgent)
    {
        // If batteryBuutAgent is null, no need to perform any role validation
        if (batteryBuutAgent == null)
        {
            _batteryBuutAgent = null;
            return; 
        }
        // check for required role
        if (!batteryBuutAgent.HasRole(new Role(REQUIRED_ROLE_BATTERYBUUTAGENT))) {
            throw new InvalidOperationException($"The given user does not have the required role: {REQUIRED_ROLE_BATTERYBUUTAGENT}");
        }

        _batteryBuutAgent = batteryBuutAgent; 
    }

    /// <summary>
    /// Maps the battery to a <see cref="BatteryDto.ViewBatteryBuutAgent"/> 
    /// </summary>
    /// <returns><see cref="BatteryDto.ViewBatteryBuutAgent"/></returns>
    public BatteryDto.ViewBatteryBuutAgent toViewBatteryBuutAgentDto(){
        return new BatteryDto.ViewBatteryBuutAgent{
            id = Id,
            name = Name,
            countBookings = CountBookings,
            listComments = ListComments,
        };
    }

    public void ChangeCurrentUser(User user)
    {
        Guard.Against.Null(user, nameof(user));
        CurrentUser = user;
    }

    public void ChangeBuutAgent(User user)
    {
        Guard.Against.Null(user, nameof(user));
        BatteryBuutAgent = user;
    }

    #endregion
}