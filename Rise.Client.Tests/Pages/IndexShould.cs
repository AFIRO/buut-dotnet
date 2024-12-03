using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Rise.Client.Components;
using Rise.Shared.Enums;

namespace Rise.Client.Pages;

using Bunit;
using Moq;
using Xunit;
using Microsoft.Extensions.Localization;
using Rise.Shared.Bookings;
using Microsoft.AspNetCore.Components.Authorization;

public class IndexShould : TestContext
{
    
    // Class fields for mocks
    private readonly Mock<IStringLocalizer<Index>> _indexLocalizerMock;
    private readonly Mock<IStringLocalizer<BoatInfo>> _boatInfoLocalizerMock;
    
    private readonly Mock<IBookingService> _bookingServiceMock;
    private readonly Mock<AuthenticationStateProvider> _authStateProviderMock;
    private readonly Mock<NavigationManager> _navManagerMock;

    // Constructor to initialize mocks
    public IndexShould()
    {
        _indexLocalizerMock = new Mock<IStringLocalizer<Index>>();
        _boatInfoLocalizerMock = new Mock<IStringLocalizer<BoatInfo>>();
        _bookingServiceMock = new Mock<IBookingService>();
        _authStateProviderMock = new Mock<AuthenticationStateProvider>();
        _navManagerMock = new Mock<NavigationManager>();

        // Register mocks with DI container
        Services.AddSingleton(_indexLocalizerMock.Object);
        Services.AddSingleton(_boatInfoLocalizerMock.Object);
        Services.AddSingleton(_bookingServiceMock.Object);
        Services.AddSingleton(_authStateProviderMock.Object);
        Services.AddSingleton(_navManagerMock.Object);
    }

    private void setNotAuthenticated()
    {
        _authStateProviderMock
            .Setup(a => a.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal())); // Not authenticated
    }

    private void setBasicBookingsSettings(int bookings)
    {
        _bookingServiceMock.Setup(s => s.GetFirstFreeTimeSlot()).ReturnsAsync(new BookingDto.ViewBookingCalender());
        _bookingServiceMock.Setup(s => s.GetAmountOfFreeTimeslotsForWeek()).ReturnsAsync(bookings);
    }

    private void setLanguage(string language)
    {
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(language);
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(language);
        
    }

    private void setLocalizationString(string key, string value)
    {
        _indexLocalizerMock
            .Setup(l => l[key])
            .Returns(() => 
                new LocalizedString(key, value)
            );
    }
    
    
    [Fact]
    public void TitleIsDisplayed()
    {
        setNotAuthenticated();
        
        _bookingServiceMock.Setup(s => s.GetFirstFreeTimeSlot()).ReturnsAsync(new BookingDto.ViewBookingCalender());
        _bookingServiceMock.Setup(s => s.GetAmountOfFreeTimeslotsForWeek()).ReturnsAsync(0);
        
        var cut = RenderComponent<Index>();
        cut.SetParametersAndRender();
        Assert.Contains("BUUT", cut.Markup);
       
    }    
    
    [Fact]
    public void HeaderTextIsDisplayedWithCorrectLocalization()
    {
        setNotAuthenticated();
        setBasicBookingsSettings(0);
        setLanguage("nl-BE");
    
        var subtitle = "HeaderText";
        var nlResult = "Deelbootjes voor Muide - Meulestede - Afrikalaan"; 

        setLocalizationString(subtitle, nlResult);
        
        var cut = RenderComponent<Index>();
        Assert.Contains(nlResult, cut.Markup);

    }    
    
    [Fact]
    public void ShowsLoadingSpinner()
    {
        _bookingServiceMock.Setup(s => s.GetFirstFreeTimeSlot()).Returns(Task.Delay(5000).ContinueWith(_ => new BookingDto.ViewBookingCalender()));
        _bookingServiceMock.Setup(s => s.GetAmountOfFreeTimeslotsForWeek()).ReturnsAsync(0);

        var cut = RenderComponent<Index>();
        Assert.Contains("spinner-border", cut.Markup);
    }

    [Fact]
    public void DisplaysNoFreeSlotsMessage()
    {
        setBasicBookingsSettings(0);
        setNotAuthenticated();
        setLanguage("nl-BE");
    
        var key0 = "RemainingSlots0";
        var nlResult0 = "Helaas, geen "; 
        setLocalizationString(key0, nlResult0);
        
        var key1 = "RemainingSlots2";
        var nlResult1 = "vrije tijdsloten deze week!"; 
        setLocalizationString(key1, nlResult1);
        

        var cut = RenderComponent<Index>();
        cut.SetParametersAndRender();
        Assert.Contains(nlResult0 + nlResult1, cut.Markup); 
    }   
    
    [Fact]
    public void DisplaysCertainAmountOfFreeSlotsMessage()
    {
        int bookings = 10;
        setBasicBookingsSettings(bookings);
        setNotAuthenticated();
        setLanguage("nl-BE");
    
        var key0 = "RemainingSlots1";
        var nlResult0 = "Nog slechts "; 
        setLocalizationString(key0, nlResult0);
        
        var key1 = "RemainingSlots2";
        var nlResult1 = "vrije tijdsloten deze week!"; 
        setLocalizationString(key1, nlResult1);
        

        var cut = RenderComponent<Index>();
        cut.SetParametersAndRender();
        Assert.Contains(nlResult0 + bookings + nlResult1, cut.Markup); 
    }

    [Fact]
    public void DisplaysNextFreeSlotDetails()
    {
        setNotAuthenticated();
        _bookingServiceMock.Setup(s => s.GetFirstFreeTimeSlot()).ReturnsAsync(new BookingDto.ViewBookingCalender
        {
            BookingDate = DateTime.Today,
            TimeSlot = TimeSlot.Morning
        });

        var key = "Morning";
        var value = "10:00";
        setLocalizationString(key, value);

        var cut = RenderComponent<Index>();
        cut.Render();
        Assert.Contains(DateTime.Today.ToString("D"), cut.Markup);
        Assert.Contains(value, cut.Markup);
    }

    [Fact]
    public void ShowsLoginAndRegisterButtonsForGuests()
    {
        setNotAuthenticated();
        setBasicBookingsSettings(0);
        var registerKey = "Register";
        var registerValue = "Registreer";
        
        setLocalizationString(registerKey, registerValue);

        var cut = RenderComponent<Index>();
        Assert.Contains("data-testid=\"login-button\"", cut.Markup);
        Assert.Contains(registerValue, cut.Markup);
    }

    [Fact]
    public void ShowsBookNowButtonForAuthenticatedUsers()
    {
        _authStateProviderMock.Setup(a => a.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity("Test"))));

        setBasicBookingsSettings(0);
        var bookKey = "BookNow";
        var bookValue = "Boek nu!";
        
        setLocalizationString(bookKey, bookValue);
        var cut = RenderComponent<Index>();
        Assert.Contains(bookValue, cut.Markup); 
    }

    [Fact]
    public void CarouselDisplaysCorrectImages()
    {
        setBasicBookingsSettings(0);
        setNotAuthenticated();
        var cut = RenderComponent<Index>();
        Assert.Contains("img/Buut_BG3.png", cut.Markup);
        Assert.Contains("img/Buut_BG4.png", cut.Markup);
    }

}