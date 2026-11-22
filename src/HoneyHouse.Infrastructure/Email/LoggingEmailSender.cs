using HoneyHouse.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace HoneyHouse.Infrastructure.Email;

/// <summary>Development email sender that writes emails to the log. Replace with an SMTP/provider implementation in production.</summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        logger.LogInformation("Email to {To}: {Subject}\n{Body}", to, subject, htmlBody);
        return Task.CompletedTask;
    }
}
