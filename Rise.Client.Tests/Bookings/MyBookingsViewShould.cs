using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using MudBlazor.Interop;
using MudBlazor.Services;
using Rise.Client.Auth;
using Rise.Client.Bookings;
using Rise.Client.Users;
using Rise.Shared.Bookings;
using Rise.Shared.Users;
using Shouldly;
using Xunit.Abstractions;

namespace Xunit.Bookings;

public class MyBookingsViewShould : TestContext
{
    private readonly ITestOutputHelper output;
    
    public MyBookingsViewShould(ITestOutputHelper outputHelper)
    {
        this.output = outputHelper;
        Services.AddXunitLogger(outputHelper);
        
        Services.AddSingleton<IUserService, FakeUserService>();
        Services.AddSingleton<IBookingService, FakeBookingService>();
        Services.AddSingleton<IDialogService>(new FakeDialogService());

        Services.AddLocalization();
        Services.AddAuthorizationCore();

        JSInterop.Setup<BoundingClientRect>("mudElementRef.getBoundingClientRect", _ => true);
    }
    //
    // [Fact]
    // public void MyBookingsViewRendersCorrectly_Auth()
    // {
    //     var fakeAuthProvider = new FakeAuthenticationProvider("auth0|123456", "Admin");
    //     Services.AddSingleton<AuthenticationStateProvider>(fakeAuthProvider);
    //     
    //     // Render the MyBookingsView component
    //     var cut = RenderComponent<MyBookingsView>();
    //
    //     // Assert: the page renders 5 future bookings without throwing errors
    //     cut.FindAll(".futureBookings tbody > tr.mud-table-row").ShouldNotBeEmpty();
    //     cut.FindAll(".futureBookings tbody > tr.mud-table-row").Count.ShouldBe(5);
    // }
    
    [Fact]
    public void MyBookingsViewDoesNotRenderCorrectly_NoAuth()
    {
        this.AddTestAuthorization().SetNotAuthorized();
        
        // Render the MyBookingsView component
        Action action = () => { RenderComponent<MyBookingsView>(); };

        // Assert: the page does not render and throws an error
        action.ShouldThrow<NullReferenceException>();
    }
    
    // [Fact]
    // public void MyBookingsViewRendersPastBookingsCorrectly()
    // {
    //     var fakeAuthProvider = new FakeAuthenticationProvider("auth0|123456", "Admin");
    //     Services.AddSingleton<AuthenticationStateProvider>(fakeAuthProvider);
    //     
    //     // Render the MyBookingsView component
    //     var cut = RenderComponent<MyBookingsView>();
    //
    //     // Assert: the page renders 3 past bookings without throwing errors
    //     cut.FindAll("div.mud-expand-panel-header")[1].Click();
    //     
    //     cut.FindAll(".pastBookings tbody > tr.mud-table-row").ShouldNotBeEmpty();
    //     cut.FindAll(".pastBookings tbody > tr.mud-table-row").Count.ShouldBe(3);
    // }
    
/**    [Fact]
    public void MyBookingsViewCancelsFutureBooking()
    {
        var fakeBookingService = new FakeBookingService();
        var fakeDialogService = new FakeDialogService
        {
            MockMessageResult = true
        };
        var fakeAuthProvider = new FakeAuthenticationProvider("auth0|123456", "Admin");
        
        Services.AddSingleton<IDialogService>(fakeDialogService);
        Services.AddSingleton<IBookingService>(fakeBookingService);
        Services.AddSingleton<AuthenticationStateProvider>(fakeAuthProvider);
        
        // Render the MyBookingsView component
        var cut = RenderComponent<MyBookingsView>();
        var button = cut.FindAll(".deleteBookingButton")[0];
        button.Click();
        var futureBookingsList = cut.FindAll(".futureBookings tbody > tr.mud-table-row");
        
        // Assert: the page renders future bookings and cancels one
        futureBookingsList.ShouldNotBeEmpty();
        futureBookingsList.Count.ShouldBe(4);
    }
    **/
    
    // [Fact]
    // public void MyBookingsViewDoesNoCancelFutureBooking()
    // {
    //     var fakeAuthProvider = new FakeAuthenticationProvider("auth0|123456", "Admin");
    //     Services.AddSingleton<AuthenticationStateProvider>(fakeAuthProvider);
    //     
    //     // Render the MyBookingsView component
    //     var cut = RenderComponent<MyBookingsView>();
    //
    //     // Assert: the page renders future bookings, tries to cancel one but decides not to
    //     cut.FindAll(".deleteBookingButton")[0].Click();
    //     
    //     cut.FindAll(".futureBookings tbody > tr.mud-table-row").ShouldNotBeEmpty();
    //     cut.FindAll(".futureBookings tbody > tr.mud-table-row").Count.ShouldBe(5);
    // }
}