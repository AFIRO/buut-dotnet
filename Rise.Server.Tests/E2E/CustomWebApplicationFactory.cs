using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
using Rise.Shared.Users;

namespace Rise.Server.Tests.E2E
{
    public class CustomWebApplicationFactory<Program> : WebApplicationFactory<Program> where Program : class
    {
        public Mock<IAuth0UserService> _mockAuth0UserService;
        private string _jwtSecretKey = "YourSuperSecretKey12345YourSuperSecretKey12345";

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var projectDir = Directory.GetCurrentDirectory();
            var configPath = Path.Combine(projectDir, "..", "..", "..", "..", "Rise.Server");

            builder.UseContentRoot(configPath);
            builder.UseEnvironment("Testing");
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole(); // Add console logging
            });
            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _mockAuth0UserService = new Mock<IAuth0UserService>();


            builder.ConfigureTestServices(services =>
            {
                // Remove the existing DbContext registration
                services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));

                // Add the in-memory database
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb").EnableSensitiveDataLogging());

                // Add the Auth0 user service to the service collection
                services.AddScoped<IAuth0UserService>(provider => _mockAuth0UserService.Object);

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
                        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtSecretKey)),
                        RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
                        // RoleClaimType = "roles" // Match the "roles" claim in your payload
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            return Task.CompletedTask;
                        },
                        OnMessageReceived = context =>
                        {
                            return Task.CompletedTask;
                        }
                    };
                });
            });
        }

        public string GenerateMockJwt(string userId, string[] roles, string? secretKey = null)
        {
            if (secretKey == null)
            {
                secretKey = _jwtSecretKey;
            }

            // Create claims
            var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId)
        };

            // Ensure the role claim uses ClaimTypes.Role
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            // Create a symmetric security key
            var securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey));

            // Create signing credentials
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Create the token
            var token = new JwtSecurityToken(
                issuer: "test-issuer",
                audience: "test-audience",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(120),
                signingCredentials: signingCredentials);

            // Return the token as a string
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    
}
