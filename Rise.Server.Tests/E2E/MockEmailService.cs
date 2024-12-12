namespace Rise.Server.Tests.E2E
{
    using System.Threading.Tasks;
    using Rise.Services.Mail;

    /// <summary>
    /// Mock email service for testing purposes.
    /// </summary>
    public class MockEmailService : IEmailService
    {
        /// <summary>
        /// Sends an email asynchronously.
        /// </summary>
        /// <param name="emailMessage">The email message to send.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task SendEmailAsync(EmailMessage emailMessage)
        {
            // Simulate successful email sending
            return Task.CompletedTask;
        }
    }
}