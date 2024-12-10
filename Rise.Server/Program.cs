using Microsoft.EntityFrameworkCore;
using Rise.Persistence;
using Rise.Persistence.Triggers;
using Rise.Services.Users;
using Rise.Shared.Users;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Auth0Net.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Rise.Server.Settings;
using Rise.Services.Bookings;
using Rise.Shared.Bookings;
using Rise.Shared.Services;
using Rise.Services.Notifications;
using Rise.Shared.Notifications;
using Rise.Services.Events;
using Rise.Services.Events.User;
using Rise.Services.Events.Booking;
using AngleSharp.Text;
using Rise.Domain.Bookings;
using Rise.Shared.Boats;
using System.Text.Json.Serialization;
using Rise.Shared.Batteries;
using Rise.Services.Batteries;
using NLog.Web;
using Rise.Server.LoggingEnrichers;
using NLog;

var builder = WebApplication.CreateBuilder(args);

// Add NLog as the logging provider
builder.Logging.ClearProviders();
builder.Host.UseNLog();

// Register HttpContextAccessor and enrichers
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UserIdEnricher>();
builder.Services.AddScoped<ClientIpAddressEnricher>();

// Configure NLog globally to include custom enrichers
LogManager.Setup().LoadConfigurationFromFile("nlog.config");



builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Add the immutable converters for System.Collections.Immutable types
        options.JsonSerializerOptions.Converters.Add(new ImmutableListJsonConverter<RoleDto>());
        // This ensures enums are serialized as strings
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                TokenUrl = new Uri($"{builder.Configuration["Auth0:Authority"]}/oauth/token"),
                AuthorizationUrl =
                    new Uri(
                        $"{builder.Configuration["Auth0:Authority"]}/authorize?audience={builder.Configuration["Auth0:Audience"]}"),
            }
        }
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "oauth2"
                }
            },
            new string[] { "openid" }
        }
    });
});

builder.Services.Configure<BookingSettings>(builder.Configuration.GetSection("BookingSettings"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.Authority = builder.Configuration["Auth0:Authority"];
    options.Audience = builder.Configuration["Auth0:Audience"];
    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = ClaimTypes.NameIdentifier
    };
});

builder.Services.AddAuth0AuthenticationClient(config =>
{
    config.Domain = builder.Configuration["Auth0:Authority"]!;
    config.ClientId = builder.Configuration["Auth0:M2MClientId"];
    config.ClientSecret = builder.Configuration["Auth0:M2MClientSecret"];
});
builder.Services.AddAuth0ManagementClient().AddManagementAccessToken();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
    options.EnableDetailedErrors();
    options.EnableSensitiveDataLogging();
    options.UseTriggers(options => options.AddTrigger<EntityBeforeSaveTrigger>());
});

// Register services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IEquipmentService<BoatDto.ViewBoat, BoatDto.NewBoat, BoatDto.UpdateBoat>, BoatService>();
builder.Services.AddScoped<IBatteryService, BatteryService>();
builder.Services.AddScoped<IAuth0UserService, Auth0UserService>();
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<BookingAllocator>();
builder.Services.AddScoped<BookingAllocationService>();

builder.Services.AddHostedService<DailyTaskService>();

// Register event dispatcher
builder.Services.AddSingleton<IEventDispatcher, EventDispatcher>();

// Register open generic handlers
builder.Services.AddScoped(typeof(IEventHandler<>), typeof(GenericEventHandler<>));

// Register specific User event handlers
builder.Services.AddScoped<IEventHandler<UserRegisteredEvent>, NotifyAdminsOnUserRegistrationHandler>();
builder.Services.AddScoped<IEventHandler<UserDeletedEvent>, NotifyAdminsOnUserDeletionHandler>();
builder.Services.AddScoped<IEventHandler<UserUpdatedEvent>, NotifyAdminsOnUserUpdateHandler>();
builder.Services.AddScoped<IEventHandler<UserValidationEvent>, NotifyUserOnUserValidationHandler>();
builder.Services.AddScoped<IEventHandler<UserRoleUpdatedEvent>, NotifyUserOnNewRolesAssignedHandler>();

// Register specific Booking event handlers
builder.Services.AddScoped<IEventHandler<BookingCreatedEvent>, NotifyOnBookingCreatedHandler>();
builder.Services.AddScoped<IEventHandler<BookingUpdatedEvent>, NotifyOnBookingUpdatedHandler>();
builder.Services.AddScoped<IEventHandler<BookingDeletedEvent>, NotifyOnBookingDeletedHandler>();


var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1.0");
        options.OAuthClientId(builder.Configuration["Auth0:BlazorClientId"]);
        options.OAuthClientSecret(builder.Configuration["Auth0:BlazorClientSecret"]);
    });
}


app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();


// Middleware to enrich logs
app.Use(async (context, next) =>
{
    IDisposable? userIdScope = null;
    IDisposable? clientIpScope = null;

    if (!context.Request.Path.StartsWithSegments("/swagger")) // Skip Swagger requests
    {
        var userIdEnricher = context.RequestServices.GetRequiredService<UserIdEnricher>();
        var clientIpEnricher = context.RequestServices.GetRequiredService<ClientIpAddressEnricher>();

        // Use ScopeContext.PushProperty to set the properties
        userIdScope = ScopeContext.PushProperty("UserId", userIdEnricher.Enrich());
        clientIpScope = ScopeContext.PushProperty("ClientIpAddress", clientIpEnricher.Enrich());
    }

    try
    {
        await next();
    }
    finally
    {
        // Dispose of the scope after the request
        userIdScope?.Dispose();
        clientIpScope?.Dispose();
    }
});


app.MapControllers();
app.MapFallbackToFile("index.html");

using (var scope = app.Services.CreateScope())
{
    // Require a DbContext from the service provider and seed the database.
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    Seeder seeder = new(dbContext);
    seeder.Seed();
}

app.Run();