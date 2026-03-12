using Chairly.Api.Features.Notifications.Infrastructure;

namespace Chairly.Tests.Features.Notifications;

public class EmailTemplateTests
{
    [Fact]
    public void BookingConfirmation_ReturnsNonEmptySubjectAndBody()
    {
        var (subject, body) = EmailTemplates.BookingConfirmation("Jan Smit", DateTimeOffset.UtcNow, "Herenknippen");

        Assert.False(string.IsNullOrEmpty(subject));
        Assert.False(string.IsNullOrEmpty(body));
    }

    [Fact]
    public void BookingConfirmation_SubjectContainsBevestiging()
    {
        var (subject, _) = EmailTemplates.BookingConfirmation("Jan Smit", DateTimeOffset.UtcNow, "Herenknippen");

        Assert.Contains("Bevestiging", subject, StringComparison.Ordinal);
    }

    [Fact]
    public void BookingConfirmation_BodyContainsClientName()
    {
        var (_, body) = EmailTemplates.BookingConfirmation("Jan Smit", DateTimeOffset.UtcNow, "Herenknippen");

        Assert.Contains("Jan Smit", body, StringComparison.Ordinal);
    }

    [Fact]
    public void BookingConfirmation_BodyContainsServiceSummary()
    {
        var (_, body) = EmailTemplates.BookingConfirmation("Jan Smit", DateTimeOffset.UtcNow, "Herenknippen, Baard trimmen");

        Assert.Contains("Herenknippen, Baard trimmen", body, StringComparison.Ordinal);
    }

    [Fact]
    public void BookingReminder_ReturnsNonEmptySubjectAndBody()
    {
        var (subject, body) = EmailTemplates.BookingReminder("Jan Smit", DateTimeOffset.UtcNow, "Herenknippen");

        Assert.False(string.IsNullOrEmpty(subject));
        Assert.False(string.IsNullOrEmpty(body));
    }

    [Fact]
    public void BookingReminder_SubjectContainsHerinnering()
    {
        var (subject, _) = EmailTemplates.BookingReminder("Jan Smit", DateTimeOffset.UtcNow, "Herenknippen");

        Assert.Contains("Herinnering", subject, StringComparison.Ordinal);
    }

    [Fact]
    public void BookingReminder_BodyContainsClientName()
    {
        var (_, body) = EmailTemplates.BookingReminder("Jan Smit", DateTimeOffset.UtcNow, "Herenknippen");

        Assert.Contains("Jan Smit", body, StringComparison.Ordinal);
    }

    [Fact]
    public void BookingCancellation_ReturnsNonEmptySubjectAndBody()
    {
        var (subject, body) = EmailTemplates.BookingCancellation("Jan Smit", DateTimeOffset.UtcNow);

        Assert.False(string.IsNullOrEmpty(subject));
        Assert.False(string.IsNullOrEmpty(body));
    }

    [Fact]
    public void BookingCancellation_SubjectContainsGeannuleerd()
    {
        var (subject, _) = EmailTemplates.BookingCancellation("Jan Smit", DateTimeOffset.UtcNow);

        Assert.Contains("geannuleerd", subject, StringComparison.Ordinal);
    }

    [Fact]
    public void BookingCancellation_BodyContainsClientName()
    {
        var (_, body) = EmailTemplates.BookingCancellation("Jan Smit", DateTimeOffset.UtcNow);

        Assert.Contains("Jan Smit", body, StringComparison.Ordinal);
    }
}
