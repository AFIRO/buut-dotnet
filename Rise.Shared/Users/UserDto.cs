using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Rise.Shared.Enums;

namespace Rise.Shared.Users;
/// <summary>
/// Data Transfer Object (DTO) representing a user with minimal info.
/// Used records for the DTO's itself, because they are immutable by design
/// Using sealed records gives a performance gain
/// </summary>
public class UserDto
{
    public class TempUser
    {
        [Required(ErrorMessage = "FirstNameRequired")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "LastNameRequired")]
        public string LastName { get; set; }
        [Required(ErrorMessage = "EmailRequired")]
        [EmailAddress(ErrorMessage = "EmailInvalid")]
        public string Email { get; set; }
        [Required(ErrorMessage = "PasswordRequired")]
        [MinLength(8, ErrorMessage = "PasswordMinLength")]
        public string Password { get; set; }
        [Required(ErrorMessage = "ConfirmPasswordRequired")]
        [Compare("Password", ErrorMessage = "PasswordNotMatch")]
        public string ConfirmPassword { get; set; }  // Add this field to your model

        [Required(ErrorMessage = "PhoneNumberRequired")]
        // [BelgianPhoneNumber]
        public string PhoneNumber { get; set; }
        public string Id { get; set; }
        // public AddressDto.CreateAddress Address { get; set; } = new();
        [MinimumAge(18, ErrorMessage = "Min18YearsOld")]
        public DateTime BirthDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "StreetRequired")]
        public StreetEnum? Street { get; set; } = null;
        /// <summary>
        /// Gets or sets the house number of the address.
        /// </summary>
        [NotNullOrEmpty]
        [RegularExpression(@"^\d+\s?[A-Za-z]?$", ErrorMessage = "House number must be a number or a number followed by a letter.")]
        [Required(ErrorMessage = "HouseNumberRequired")]
        public string? HouseNumber { get; set; } = null;
        /// <summary>
        /// Gets or sets the optional bus number for the address.
        /// </summary>
        public string? Bus { get; set; }
    }
    public sealed record UserBase
    {

        public string Id { get; init; }
        public string FirstName { get; init; }
        public string LastName { get; init; }
        public string Email { get; init; }
        public bool IsDeleted { get; init; }
        public ImmutableList<RoleDto> Roles { get; init; } = ImmutableList<RoleDto>.Empty;


        // Constructor to initialize everything
        public UserBase(string id, string firstName, string lastName, string email,
            ImmutableList<RoleDto>? roles = null)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            Roles = roles ?? ImmutableList<RoleDto>.Empty;
        }
    }

    /// <summary>
    /// DTO for showing users details in table on Users page
    /// </summary>
    public sealed record UserDetails
    {
        public string Id { get; init; }
        public string FirstName { get; init; }
        public string LastName { get; init; }
        public string Email { get; init; }
        public AddressDto.GetAdress Address { get; init; }
        public string PhoneNumber { get; init; }
        public ImmutableList<RoleDto> Roles { get; init; } = ImmutableList<RoleDto>.Empty;
        public DateTime BirthDate { get; init; } = DateTime.Now;

        // Parameterless constructor for deserialization
        public UserDetails() { }

        public UserDetails(string id, string firstName, string lastName, string email, string phoneNumber,
            AddressDto.GetAdress address, ImmutableList<RoleDto>? roles = null, DateTime? birthDate = null)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            PhoneNumber = phoneNumber;
            Address = address;
            Roles = roles ?? ImmutableList<RoleDto>.Empty;
            BirthDate = birthDate ?? DateTime.Now;
        }
    }

    /// <summary>
    /// DTO used for registrationform
    /// </summary>
    public sealed record RegistrationUser(
        string FirstName,
        string LastName,
        string Email,
        string PhoneNumber,
        string? Password,
        string? Id,
        AddressDto.GetAdress? Address,
        DateTime? BirthDate = null)
    {
        public DateTime? BirthDate { get; init; } = BirthDate ?? DateTime.UtcNow;
    };

    /// <summary>
    /// DTO used to update a User in the DB
    /// Almost identical to RegistrationUser, but Id is now also included
    /// </summary>
    public sealed record UpdateUser
    {
        public string Id { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Email { get; init; }
        public string? Password { get; init; }
        public DateTime? BirthDate { get; init; }
        public AddressDto.UpdateAddress? Address { get; init; }
        public ImmutableList<RoleDto>? Roles { get; init; }
        public string? PhoneNumber { get; init; }

        // public UpdateUser(string id, string firstName, string lastName, string email, string phoneNumber,
        //     AddressDto.UpdateAddress address, ImmutableList<RoleDto>? roles = null, DateTime? birthDate = null)
        // {
        //     Id = id;
        //     FirstName = firstName;
        //     LastName = lastName;
        //     Email = email;
        //     PhoneNumber = phoneNumber;
        //     Address = address;
        //     Roles = roles ?? ImmutableList<RoleDto>.Empty;
        //     BirthDate = birthDate ?? DateTime.Now;
        // }
    }

    /// <summary>
    /// DTO used for showing users in the table on the AuthUsers page
    /// </summary>
    public sealed record Auth0User(string Email, string FirstName, string LastName, bool Blocked);


}