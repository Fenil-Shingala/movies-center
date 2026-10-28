using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MoviesCenter.Application.Common.Interfaces;

namespace MoviesCenter.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        // Log cleanly for development and audit trails
        _logger.LogInformation("Dispatched notification email to {ToEmail}. Subject: '{Subject}'. Body snippet: {Snippet}",
            toEmail,
            subject,
            htmlBody.Length > 120 ? htmlBody[..120] + "..." : htmlBody);

        // If Postmark / SMTP is configured in the future, trigger HTTP request here
        return Task.CompletedTask;
    }
}
