using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

#pragma warning disable CA1812 // Instantiated via DI
namespace Chairly.Api.Features.Notifications.Infrastructure;

internal sealed class SmtpEmailSender(IOptions<SmtpSettings> options) : IEmailSender
{
    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var settings = options.Value;

        using var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody,
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(settings.Host, settings.Port, MailKit.Security.SecureSocketOptions.None, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(settings.Username))
        {
            await client.AuthenticateAsync(settings.Username, settings.Password, cancellationToken).ConfigureAwait(false);
        }

        await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
        await client.DisconnectAsync(quit: true, cancellationToken).ConfigureAwait(false);
    }
}
#pragma warning restore CA1812
