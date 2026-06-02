using Ganss.Xss;

#pragma warning disable CA1812 // Instantiated via DI
namespace Chairly.Api.Features.Newsletters.Infrastructure;

internal sealed class NewsletterHtmlSanitizer : INewsletterHtmlSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public NewsletterHtmlSanitizer()
    {
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedTags.Clear();
        foreach (var tag in new[]
        {
            "p", "br", "strong", "em", "u", "s", "blockquote",
            "ul", "ol", "li",
            "h1", "h2", "h3", "h4",
            "a", "img", "span", "div",
        })
        {
            _sanitizer.AllowedTags.Add(tag);
        }

        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedAttributes.Add("href");
        _sanitizer.AllowedAttributes.Add("src");
        _sanitizer.AllowedAttributes.Add("alt");
        _sanitizer.AllowedAttributes.Add("title");
        _sanitizer.AllowedAttributes.Add("target");

        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowedSchemes.Add("http");
        _sanitizer.AllowedSchemes.Add("https");
        _sanitizer.AllowedSchemes.Add("mailto");
    }

    public string Sanitize(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        return _sanitizer.Sanitize(html);
    }
}
#pragma warning restore CA1812
