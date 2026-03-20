using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

#pragma warning disable CA1812 // Instantiated via DI
namespace Chairly.Api.Features.Notifications.Infrastructure;

internal sealed class SmtpEmailSender(IOptions<SmtpSettings> options) : IEmailSender
{
    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, EmailAttachment? attachment = null, CancellationToken cancellationToken = default)
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

        if (attachment is not null)
        {
            await bodyBuilder.Attachments.AddAsync(
                attachment.FileName,
                new MemoryStream(attachment.Content),
                ContentType.Parse(attachment.ContentType),
                cancellationToken).ConfigureAwait(false);
        }

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
