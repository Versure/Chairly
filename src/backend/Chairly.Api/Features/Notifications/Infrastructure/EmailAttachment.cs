namespace Chairly.Api.Features.Notifications.Infrastructure;

internal sealed record EmailAttachment(string FileName, string ContentType, byte[] Content);
