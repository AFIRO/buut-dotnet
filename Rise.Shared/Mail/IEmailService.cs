public interface IEmailService
{
     Task SendEmailAsync(EmailMessageDto emailMessage);
}