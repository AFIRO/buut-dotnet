using Rise.Domain.Bookings;
using Rise.Domain.Notifications;

namespace Rise.Domain.Users;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents a user entity in the system
/// </summary>
public class User : Entity
{
    #region Fields

    private string _id = Guid.NewGuid().ToString();

    private string _firstName = default!;
    private string _lastName = default!;

    private string _email = default!;

    // private string _password = default!;
    private DateTime _birthDate;
    private Address _address = default!;
    private List<Role> _roles = [];
    private string _phoneNumber = default!;
    private List<Booking> _bookings = [];

    private List<Notification> _notifications = [];

    private Battery? _currentBattery;
    private Battery? _isBuutAgentOfBattery;
    

    #endregion

    #region Constructors

    /// <summary>
    ///Private constructor for Entity Framework Core
    /// </summary>
    private User()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class with the specified details.
    /// </summary>
    /// <param name="id">The id of the user in the db</param>
    /// <param name="firstName">The first name of the user.</param>
    /// <param name="lastName">The last name of the user.</param>
    /// <param name="email">The email address of the user.</param>
    /// <param name="birthDate">The birth date of the user.</param>
    /// <param name="address">The address of the user.</param>
    /// <param name="phoneNumber">The phone number of the user.</param>
    public User(string id, string firstName, string lastName, string email, DateTime birthDate, Address address,
        string phoneNumber)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        BirthDate = birthDate;
        Address = address;
        PhoneNumber = phoneNumber;
    }

    #endregion


    #region Properties

    public string Id
    {
        get => _id;
        set => _id = Guard.Against.NullOrWhiteSpace(value, nameof(Id));
    }

    /// <summary>
    /// Gets or sets the first name of the user.
    /// </summary>
    public string FirstName
    {
        get => _firstName;
        set => _firstName = Guard.Against.NullOrWhiteSpace(value, nameof(FirstName));
    }

    /// <summary>
    /// Gets or sets the last name of the user.
    /// </summary>
    public string LastName
    {
        get => _lastName;
        set => _lastName = Guard.Against.NullOrWhiteSpace(value, nameof(LastName));
    }

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    public string Email
    {
        get => _email;
        set
        {
            // Validate using Guard clause
            _email = Guard.Against.NullOrWhiteSpace(value, nameof(Email));

            // Validate using data annotation attribute (Optional but good to check)
            var emailValidationAttribute = new EmailAddressAttribute();
            if (!emailValidationAttribute.IsValid(value))
            {
                throw new ArgumentException("Invalid email address format.", nameof(Email));
            }
        }
    }

    /// <summary>
    /// Gets or sets the birth date of the user.
    /// </summary>
    public DateTime BirthDate
    {
        get => _birthDate;
        set => _birthDate = Guard.Against.Default(value, nameof(BirthDate));
    }

    /// <summary>
    /// Gets or sets the address of the user.
    /// </summary>
    public Address Address
    {
        get => _address;
        set => _address = Guard.Against.Null(value, nameof(Address));
    }

    /// <summary>
    /// Gets the roles associated with the user.
    /// </summary>
    public List<Role> Roles => _roles;

    /// <summary>
    /// Gets the bookings associated with the user.
    /// </summary>
    public IReadOnlyList<Booking> Bookings => _bookings;

    public List<Notification> Notifications => _notifications;

    /// <summary>
    /// Gets or sets the phone number of the user.
    /// </summary>
    public string PhoneNumber
    {
        get => _phoneNumber;
        set => _phoneNumber = Guard.Against.NullOrWhiteSpace(value, nameof(PhoneNumber));
    }

    public string? IsBuutAgentOfBatteryId;

    public Battery? IsBuutAgentOfBattery
    {
        get => _isBuutAgentOfBattery;
        set => Guard.Against.Null(value, nameof(IsBuutAgentOfBattery));
    }

    public string? CurrentBatteryId;

    public Battery? CurrentBattery
    {
        get => _currentBattery;
        set => Guard.Against.Null(value, nameof(CurrentBattery));
    }

    #endregion


    #region Methods

    /// <summary>
    /// Adds a role to the user.
    /// </summary>
    /// <param name="role">The role to add.</param>
    public void AddRole(Role role)
    {
        Guard.Against.Null(role, nameof(role));
        _roles.Add(role);
    }

    /// <summary>
    /// Removes a role from the user.
    /// </summary>
    /// <param name="role">The role to remove.</param>
    public void RemoveRole(Role role)
    {
        Guard.Against.Null(role, nameof(role));
        _roles.Remove(role);
    }

    /// <summary>
    /// Adds a booking to the user.
    /// </summary>
    /// <param name="booking">The booking to add.</param>
    public void AddBooking(Booking booking)
    {
        Guard.Against.Null(booking, nameof(booking));
        _bookings.Add(booking);
    }

    /// <summary>
    /// Removes a booking from the user.
    /// </summary>
    /// <param name="booking">The booking to remove.</param>
    public void RemoveBooking(Booking booking)
    {
        Guard.Against.Null(booking, nameof(booking));
        _bookings.Remove(booking);
    }

    // /// <summary>
    // /// Marks the user as deleted (soft delete).
    // /// </summary>
    // public void SoftDelete()
    // {
    //     IsDeleted = true;
    // }

    /// <summary>
    /// Reactivates a previously deleted user.
    /// </summary>
    public void Activate()
    {
        IsDeleted = false;
    }

    #endregion
}