using System;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Shouldly;
using Rise.Shared.Users;
using Rise.Shared.Enums;
using Moq;

namespace Rise.Client.Users;

public class RegistrationShould : TestContext
{
    private readonly ITestOutputHelper output;

    public RegistrationShould(ITestOutputHelper outputHelper)
    {
        this.output = outputHelper;
        Services.AddXunitLogger(outputHelper);
        Services.AddScoped<IUserService, FakeUserService>();
        Services.AddLocalization();


    // Properly mock JavaScript interop for autofill
    JSInterop.SetupVoid("addAutofillEvent", _ => true);
    JSInterop.SetupVoid("promptAutofill");
    }


    [Fact]
    public void RegisterPageRendersCorrectly()
    {
        // Render the Register component
        var cut = RenderComponent<Register>();

        // Assert: form fields are rendered
        cut.Find("input#FirstName").ShouldNotBeNull();
        cut.Find("input#LastName").ShouldNotBeNull();
        cut.Find("input#Email").ShouldNotBeNull();
        cut.Find("input#PhoneNumber").ShouldNotBeNull();
        cut.Find("input#Password").ShouldNotBeNull();
        cut.Find("input#ConfirmPassword").ShouldNotBeNull();
        cut.Find("input#BirthDate").ShouldNotBeNull();
        cut.Find("input#Street").ShouldNotBeNull();
        cut.Find("input#HouseNumber").ShouldNotBeNull();
        cut.Find("input#Bus").ShouldNotBeNull();
    }

    [Fact]
    public void SubmitForm_WithEmptyFields_ShowsValidationErrors()
    {
        // Render the Register component
        var cut = RenderComponent<Register>();

        // Act: Try to submit the form without filling any data
        cut.Find("input#flexCheckDefault").Change(true);
        cut.Find("button[type='submit']").Click();

        // Assert: Validation errors should be displayed
        cut.FindAll("div.validation-message").Count.ShouldBeGreaterThan(0); // There should be validation errors
    }

    [Fact]
    public void SubmittingValidForm_ShouldCallCreateUserService()
    {
        // Arrange
        var user = new UserDto.RegistrationUser
        (
            "John",
            "Doe",
            "john.doe@example.com",
            "+32478334345",
            "Password123",
            "auth0|123456",
            new AddressDto.CreateAddress
            {
                Street = StreetEnum.AFRIKALAAN,
                HouseNumber = "15",
                Bus = "B"
            },
            DateTime.Now.AddYears(-20) // Valid birthdate
        );

        // Render the Register component
        var cut = RenderComponent<Register>();

        // Act: Fill in the form with valid data
        cut.Find("input#FirstName").Change(user.FirstName);
        cut.Find("input#LastName").Change(user.LastName);
        cut.Find("input#Email").Change(user.Email);
        cut.Find("input#PhoneNumber").Change(user.PhoneNumber);
        cut.Find("input#Password").Change(user.Password);
        cut.Find("input#ConfirmPassword").Change(user.Password);
        cut.Find("input#BirthDate").Change(user.BirthDate?.ToString("yyyy-MM-dd") ?? "");
        cut.Find("input#HouseNumber").Change(user.Address.HouseNumber);
        cut.Find("input#Bus").Change(user.Address.Bus);
        cut.Find("input#Street").Input(user.Address.Street.ToString().ToLower());

        cut.Find("ul.street-list li").Click();

        // Submit the form
        cut.Find("input#flexCheckDefault").Change(true);
        cut.Find("button[type='submit']").Click();

        cut.FindAll("div.validation-message").Count.ShouldBeLessThanOrEqualTo(0); // There should be no validation errors

        // Assert: Ensure CreateUserAsync was called
        var header = cut.Find("h2");
        Assert.NotNull(header);
        Assert.Contains("ThankForRegistering", header.TextContent);
    }

    [Theory]
    [InlineData("FirstName", "FirstNameRequired")]
    [InlineData("LastName", "LastNameRequired")]
    [InlineData("Email", "EmailRequired")]
    [InlineData("PhoneNumber", "PhoneNumberRequired")]
    [InlineData("Password", "PasswordRequired")]
    [InlineData("ConfirmPassword", "ConfirmPasswordRequired")]
    [InlineData("BirthDate", "The BirthDate field must be a date.")]
    [InlineData("Street", "StreetRequired")]
    [InlineData("HouseNumber", "StreetRequired")]
    public void SubmitForm_WithMissingField_ShowsSpecificValidationError(string inputId, string expectedMessage)
    {
        var cut = RenderComponent<Register>();

        // Fill all fields except one to check validation message for that field
        cut.Find("input#FirstName").Change("John");
        cut.Find("input#LastName").Change("Doe");
        cut.Find("input#Email").Change("john.doe@example.com");
        cut.Find("input#PhoneNumber").Change("+32478334345");
        cut.Find("input#Password").Change("Password123");
        cut.Find("input#ConfirmPassword").Change("Password123");
        cut.Find("input#BirthDate").Change(DateTime.Now.AddYears(-20).ToString("yyyy-MM-dd"));
        cut.Find("input#Street").Input("afrikalaan");
        cut.Find("input#HouseNumber").Change("15");
        cut.Find("input#flexCheckDefault").Change(true);

        // Clear the specific input to trigger validation error for it
        cut.Find($"input#{inputId}").Change("");

        // Submit the form
        cut.Find("button[type='submit']").Click();

        // Assert: Check that the expected validation message is shown
        cut.Find("div.validation-message").TextContent.ShouldContain(expectedMessage);
    }
}
