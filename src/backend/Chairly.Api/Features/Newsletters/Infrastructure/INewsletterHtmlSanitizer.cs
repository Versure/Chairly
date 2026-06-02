namespace Chairly.Api.Features.Newsletters.Infrastructure;

internal interface INewsletterHtmlSanitizer
{
    string Sanitize(string html);
}
