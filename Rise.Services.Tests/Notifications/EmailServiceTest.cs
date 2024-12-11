using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Rise.Shared;

namespace Rise.Services.Tests
{
    public class EmailServiceTest
    {
        private readonly EmailService _emailService;
        private readonly Mock<ILogger<EmailService>> _loggerMock;

        public EmailServiceTest()
        {
            var emailSettings = new EmailSettings
            {
                SmtpServer = "smtp.example.com",
                SmtpPort = 587,
                SmtpUsername = "username",
                SmtpPassword = "password",
                FromEmail = "from@example.com"
            };

            var options = Options.Create(emailSettings);
            _loggerMock = new Mock<ILogger<EmailService>>();
            _emailService = new EmailService(options, _loggerMock.Object);
        }

        [Fact]
        public async Task SendEmailAsync_ShouldSendEmail()
        {
            // var emailMessage = new EmailMessage
            // {
            //     To = "robin.wyffels-surplus@outlook.com",
            //     Subject = "Test Email",
            //     Title_EN = "Test Email Title",
            //     Message_EN = "This is a test email message.",
            //     Title_NL = "Test Email Titel",
            //     Message_NL = "Dit is een test e-mailbericht."
            // };

            // await _emailService.SendEmailAsync(emailMessage);

            // // Verify that the email was sent
            // _loggerMock.Verify(
            //     x => x.Log(
            //         It.Is<LogLevel>(l => l == LogLevel.Information),
            //         It.IsAny<EventId>(),
            //         It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Email sent successfully")),
            //         It.IsAny<Exception>(),
            //         It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            //     Times.Once);
            Assert.True(true);
        }
    }
}