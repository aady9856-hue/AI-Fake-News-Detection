using Microsoft.AspNetCore.Identity.UI.Services;

namespace FakeNewsDetection.Services
{
    public class NoOpEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Email confirmation is disabled for this project.
            return Task.CompletedTask;
        }
    }
}