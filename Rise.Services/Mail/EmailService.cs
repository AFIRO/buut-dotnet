using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rise.Shared;

public class EmailService : IEmailService
{
    // private readonly SmtpClient _smtpClient;
    private readonly EmailSettingsDto _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettingsDto> emailSettings, ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
        if (string.IsNullOrWhiteSpace(_emailSettings.SmtpServer))
        {
            _logger.LogError("SMTP server is not configured.");
            _logger.LogError("Email service will not be available. on SMTP Server: {smtpServer}", _emailSettings.SmtpServer);
            throw new ArgumentException("SMTP server is not configured.");
            
        }
    }

    public async Task SendEmailAsync(EmailMessageDto emailMessage)
    {
            using var _smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
            {
                Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword),
                EnableSsl = true,
            };
            
            var body = $@"
                <h1>{emailMessage.Title_EN}</h1>
                <p>{emailMessage.Message_EN}</p>
                <h1>{emailMessage.Title_NL}</h1>
                <p>{emailMessage.Message_NL}</p>";

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail),
                Subject = emailMessage.Subject,
                Body = body,
                IsBodyHtml = true,
            };
            mailMessage.To.Add(emailMessage.To);
            _logger.LogInformation("Email service accessing on SMTP Server: {smtpServer}", _emailSettings.SmtpServer);
        try
        {
            await _smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent to {to}", emailMessage.To);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error sending email: {message}", ex.Message);
            throw;
        }
    }
}