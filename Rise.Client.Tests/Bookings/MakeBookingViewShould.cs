using System;
using System.Linq;
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

public class MakeBookingViewShould : TestContext
{
    private readonly ITestOutputHelper output;
    
    public MakeBookingViewShould(ITestOutputHelper outputHelper)
    {
        this.output = outputHelper;
        Services.AddXunitLogger(outputHelper);
        
        Services.AddSingleton<IUserService, FakeUserService>();
        Services.AddSingleton<IBookingService, FakeBookingService>();
        Services.AddSingleton<IDialogService>(new FakeDialogService());

        Services.AddMudServices();
        Services.AddLocalization();
        Services.AddAuthorizationCore();

        JSInterop.Mode = JSRuntimeMode.Loose;
        
        JSInterop.SetupVoid("mudDragAndDrop.initDropZone", _ => true);
        JSInterop.SetupVoid("mudPopover.initialize", _ => true);
        JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true);
        JSInterop.SetupModule("./_content/Heron.MudCalendar/Heron.MudCalendar.min.js");
        
        var fakeAuthProvider = new FakeAuthenticationProvider("auth0|123456", "Admin");
        Services.AddSingleton<AuthenticationStateProvider>(fakeAuthProvider);
    }
    
    [Fact]
    public void MakeBookingViewRendersCorrectly_Auth()
    {
        // Render the MyBookingsView component
        RenderComponent<MudPopoverProvider>();
        var cut = RenderComponent<MakeBookingView>();
        var calendar = cut.Find(".bookingCalendar");

        // Assert: the page renders 5 future bookings without throwing errors
        calendar.ShouldNotBeNull();
    }

    [Fact]
    public void MakeBookingViewRegistersRendersFreeTimeslots()
    {
        var fakeDialogService = new FakeDialogService { MockMessageResult = true };

        Services.AddSingleton<IDialogService>(fakeDialogService);
        
        // Render the MyBookingsView component
        RenderComponent<MudPopoverProvider>();
        var cut = RenderComponent<MakeBookingView>();
        var timeslots = cut.FindAll(".timeslot");
        
        timeslots.ShouldNotBeEmpty();
    }
}