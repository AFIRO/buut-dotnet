public interface IEmailService
{
     Task SendEmailAsync(EmailMessage emailMessage);
}