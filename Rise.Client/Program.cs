using System.Globalization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Rise.Client;
using Rise.Client.Users;
using Rise.Client.Auth;
using Rise.Client.Bookings;
using Rise.Client.Notifications;
using Rise.Shared.Users;
using Rise.Shared.Bookings;
using Rise.Shared.Notifications;
using MudBlazor.Services;
using Rise.Shared.Boats;
using Rise.Client.Batteries;
using Rise.Shared.Batteries;


var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddAuthorizationCore();

// Load configuration settings
var config = builder.Configuration.GetSection("Auth0Settings");
// Register HttpClient with BaseAddress or settings from config
builder.Services.AddSingleton(config);
// Register the custom AuthenticationStateProvider
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

builder.Services.AddMudServices();

builder.Services.AddLocalization(Options => Options.ResourcesPath = "Resources.Labels");

// Register CustomAuthorizationMessageHandler for requests that need authorization
builder.Services.AddScoped<CustomAuthorizationMessageHandler>();

builder.Services.AddScoped<NotificationStateService>();

builder.Services.AddHttpClient<IUserService, UserService>(client =>
{
    client.BaseAddress = new Uri($"{builder.HostEnvironment.BaseAddress}api/");
}).AddHttpMessageHandler<CustomAuthorizationMessageHandler>();

builder.Services.AddHttpClient<IBookingService, BookingService>(client =>
{
    client.BaseAddress = new Uri($"{builder.HostEnvironment.BaseAddress}api/");
}).AddHttpMessageHandler<CustomAuthorizationMessageHandler>();

builder.Services.AddHttpClient<INotificationService, NotificationService>(client =>
{
    client.BaseAddress = new Uri($"{builder.HostEnvironment.BaseAddress}api/");
}).AddHttpMessageHandler<CustomAuthorizationMessageHandler>();

builder.Services.AddHttpClient<IEquipmentService<BoatDto.ViewBoat, BoatDto.NewBoat, BoatDto.UpdateBoat>, Rise.Client.Boats.BoatService>(client =>
{
    client.BaseAddress = new Uri($"{builder.HostEnvironment.BaseAddress}api/");
}).AddHttpMessageHandler<CustomAuthorizationMessageHandler>();

builder.Services.AddHttpClient<IEquipmentService<BatteryDto.ViewBattery, BatteryDto.NewBattery, BatteryDto.UpdateBattery>, BatteryService>(client =>
{
    client.BaseAddress = new Uri($"{builder.HostEnvironment.BaseAddress}api/");
}).AddHttpMessageHandler<CustomAuthorizationMessageHandler>();

builder.Services.AddHttpClient<IBatteryService, BatteryService>(client =>
{
    client.BaseAddress = new Uri($"{builder.HostEnvironment.BaseAddress}api/");
}).AddHttpMessageHandler<CustomAuthorizationMessageHandler>();
var host = builder.Build();


// Set the culture
var jsInterop = host.Services.GetRequiredService<IJSRuntime>();
var result = await jsInterop.InvokeAsync<string>("blazorCulture.get");

// If no culture is found in the browser, set a default (e.g., "en-US").
var culture = new CultureInfo(result ?? "en-US");

CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RunAsync();