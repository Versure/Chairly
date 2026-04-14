using System.Text.RegularExpressions;

namespace Chairly.Api.Features.Newsletters;

internal static partial class NewsletterBodyValidator
{
    public static bool IsEffectivelyEmpty(string? sanitisedHtml)
    {
        if (string.IsNullOrWhiteSpace(sanitisedHtml))
        {
            return true;
        }

        var stripped = HtmlTagRegex().Replace(sanitisedHtml, string.Empty);
        return string.IsNullOrWhiteSpace(stripped);
    }

    [GeneratedRegex("<[^>]+>", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex HtmlTagRegex();
}
