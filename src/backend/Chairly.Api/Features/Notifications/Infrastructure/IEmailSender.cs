namespace Chairly.Api.Features.Notifications.Infrastructure;

internal interface IEmailSender
{
    Task SendAsync(string toEmail, string toName, string subject, string htmlBody, EmailAttachment? attachment = null, CancellationToken cancellationToken = default);
}
