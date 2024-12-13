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
    public class CustomWebApplicationFactory<Program> : WebApplicationFactory<Program> where Program : class
    {
        public Mock<IAuth0UserService> mockAuth0UserService { get; private set; }
        public Mock<IEmailService> mockEmailService { get; private set; }
        private readonly string _jwtSecretKey = "YourSuperSecretKey12345YourSuperSecretKey12345";
        private readonly string _issuer = "test-issuer";
        private readonly string _audience = "test-audience";

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
                        ValidIssuer = _issuer,
                        ValidateAudience = true,
                        ValidAudience = _audience,
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


        public string GenerateJwtToken(string name, string role, string id, bool badIssuer = false, bool badAudience = false)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecretKey);
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
                Audience = _audience + (badAudience ? "bad" : ""), // add bad to audience if badAudience is true
                Issuer = _issuer + (badIssuer ? "bad" : "") // add bad to issuer if badIssuer is true
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }


}
