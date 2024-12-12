using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Rise.Persistence;
using Rise.Services.Mail;
using Rise.Shared;
using Rise.Shared.Users;

namespace Rise.Server.Tests.E2E
{
    // public class CustomWebApplicationFactory<Program> : WebApplicationFactory<Program> where Program : class
    // {
    //     public Mock<IAuth0UserService> _mockAuth0UserService;
    //     private string _jwtSecretKey = "YourSuperSecretKey12345YourSuperSecretKey12345";

    //     protected override IHost CreateHost(IHostBuilder builder)
    //     {
    //         var projectDir = Directory.GetCurrentDirectory();
    //         var configPath = Path.Combine(projectDir, "..", "..", "..", "..", "Rise.Server");

    //         builder.UseContentRoot(configPath);
    //         builder.UseEnvironment("Testing");
    //         builder.ConfigureLogging(logging =>
    //         {
    //             logging.ClearProviders();
    //             logging.AddConsole(); // Add console logging
    //             logging.SetMinimumLevel(LogLevel.Debug);
    //         });
    //         return base.CreateHost(builder);
    //     }

    //     protected override void ConfigureWebHost(IWebHostBuilder builder)
    //     {
    //         _mockAuth0UserService = new Mock<IAuth0UserService>();


    //         builder.ConfigureTestServices(services =>
    //         {
    //             // Remove the existing DbContext registration
    //             services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));

    //             // Add the in-memory database
    //             services.AddDbContext<ApplicationDbContext>(options =>
    //                 options.UseInMemoryDatabase("TestDb").EnableSensitiveDataLogging());

    //             // Add the Auth0 user service to the service collection
    //             services.AddScoped<IAuth0UserService>(provider => _mockAuth0UserService.Object);

    //             services.RemoveAll(typeof(JwtBearerOptions));

    //             services.AddAuthentication(options =>
    //             {
    //                 options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    //                 options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    //             }).AddJwtBearer(options =>
    //             {
    //                 options.TokenValidationParameters = new TokenValidationParameters
    //                 {
    //                     ValidateIssuer = true,
    //                     ValidIssuer = "test-issuer",
    //                     ValidateAudience = true,
    //                     ValidAudience = "test-audience",
    //                     ValidateLifetime = false,
    //                     IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtSecretKey)),
    //                     RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
    //                     // RoleClaimType = "roles" // Match the "roles" claim in your payload
    //                 };

    //                 options.Events = new JwtBearerEvents
    //                 {
    //                     OnAuthenticationFailed = context =>
    //                     {
    //                         return Task.CompletedTask;
    //                     },
    //                     OnTokenValidated = context =>
    //                     {
    //                         return Task.CompletedTask;
    //                     },
    //                     OnMessageReceived = context =>
    //                     {
    //                         return Task.CompletedTask;
    //                     }
    //                 };
    //             });
    //                // Configure EmailSettings
    //         var emailSettings = new EmailSettings
    //         {
    //             SmtpServer = "smtp.testserver.com",
    //             SmtpPort = 587,
    //             SmtpUsername = "testuser",
    //             SmtpPassword = "testpassword",
    //             FromEmail = "test@example.com"
    //         };
    //         services.Configure<EmailSettings>(options =>
    //         {
    //             options.SmtpServer = emailSettings.SmtpServer;
    //             options.SmtpPort = emailSettings.SmtpPort;
    //             options.SmtpUsername = emailSettings.SmtpUsername;
    //             options.SmtpPassword = emailSettings.SmtpPassword;
    //             options.FromEmail = emailSettings.FromEmail;
    //         });

    //         // Add the EmailService to the service collection
    //         services.AddScoped<IEmailService, EmailService>();
    //         });

    //     }

    //     public string GenerateMockJwt(string userId, string[] roles, string? secretKey = null)
    //     {
    //         if (secretKey == null)
    //         {
    //             secretKey = _jwtSecretKey;
    //         }

    //         // Create claims
    //         var claims = new List<Claim>
    //     {
    //         new Claim(JwtRegisteredClaimNames.Sub, userId)
    //     };

    //         // Ensure the role claim uses ClaimTypes.Role
    //         claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

    //         // Create a symmetric security key
    //         var securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey));

    //         // Create signing credentials
    //         var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    //         // Create the token
    //         var token = new JwtSecurityToken(
    //             issuer: "test-issuer",
    //             audience: "test-audience",
    //             claims: claims,
    //             expires: DateTime.UtcNow.AddMinutes(120),
    //             signingCredentials: signingCredentials);

    //         // Return the token as a string
    //         return new JwtSecurityTokenHandler().WriteToken(token);
    //     }
    // }
    public class CustomWebApplicationFactory<Program> : WebApplicationFactory<Program> where Program : class
    {
        public Mock<IAuth0UserService> mockAuth0UserService { get; private set; }
        public Mock<IEmailService> mockEmailService { get; private set; }
        private readonly string _jwtSecretKey = "YourSuperSecretKey12345YourSuperSecretKey12345";

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var projectDir = Directory.GetCurrentDirectory();
            var configPath = Path.Combine(projectDir, "..", "..", "..", "..", "Rise.Server");

            builder.UseContentRoot(configPath);
            builder.UseEnvironment("Testing");
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Debug);
            });

            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            mockAuth0UserService = new Mock<IAuth0UserService>();
            mockEmailService = new Mock<IEmailService>();

            mockEmailService
                    .Setup(service => service.SendEmailAsync(It.IsAny<EmailMessage>()))
                    .Returns(Task.CompletedTask);

            builder.ConfigureTestServices(services =>
            {
                // Replace database context
                services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb").EnableSensitiveDataLogging());
                
                // delete services to replace
                services.RemoveAll<IAuth0UserService>();
                services.RemoveAll<IEmailService>();
                // Mock dependencies
                services.AddSingleton<IAuth0UserService>(_ => mockAuth0UserService.Object);
                services.AddSingleton<IEmailService>(_ => mockEmailService.Object);

                // Add JWT authentication
                services.RemoveAll(typeof(JwtBearerOptions));
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                }).AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = "test-issuer",
                        ValidateAudience = true,
                        ValidAudience = "test-audience",
                        ValidateLifetime = false,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecretKey)),
                        RoleClaimType = ClaimTypes.Role
                    };
                });

                // Configure other services
                // ConfigureEmailService(services);
            });
        }

        private void ConfigureEmailService(IServiceCollection services)
        {
            var emailSettings = new EmailSettings
            {
                SmtpServer = "smtp.testserver.com",
                SmtpPort = 587,
                SmtpUsername = "testuser",
                SmtpPassword = "testpassword",
                FromEmail = "test@example.com"
            };

            services.Configure<EmailSettings>(_ =>
            {
                _.SmtpServer = emailSettings.SmtpServer;
                _.SmtpPort = emailSettings.SmtpPort;
                _.SmtpUsername = emailSettings.SmtpUsername;
                _.SmtpPassword = emailSettings.SmtpPassword;
                _.FromEmail = emailSettings.FromEmail;
            });

            services.AddScoped<IEmailService, EmailService>();
        }


        public string GenerateJwtToken(string name, string role, string id)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("YourSuperSecretKey12345YourSuperSecretKey12345");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Role, role),
            new Claim (ClaimTypes.NameIdentifier, id)
        }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Audience = "test-audience",
                Issuer = "test-issuer"
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }


}
