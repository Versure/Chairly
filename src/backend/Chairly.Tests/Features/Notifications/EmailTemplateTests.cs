using Chairly.Api.Features.Notifications.Infrastructure;

namespace Chairly.Tests.Features.Notifications;

public class EmailTemplateTests
{
    [Fact]
    public void BookingConfirmation_ReturnsNonEmptySubjectAndBody()
    {
        var (subject, body) = EmailTemplates.BookingConfirmation("Jan Smit", DateTimeOffset.UtcNow, "Herenknippen", "Kapsalon De Knip");

        Assert.False(string.IsNullOrEmpty(subject));
        Assert.False(string.IsNullOrEmpty(body));
    }

    [Fact]
    public void BookingConfirmation_SubjectContainsBevestiging()
    {
        var (subject, _) = EmailTemplates.BookingConfirmation("Jan Smit", DateTimeOffset.UtcNow, "Herenknippen", "Kapsalon De Knip");

        Assert.Contains("Bevestiging", subject, StringComparison.Ordinal);
    }

    [Fact]
    public void BookingConfirmation_BodyContainsClientName()
    {
        var (_, body) = EmailTemplates.BookingConfirmation("Jan Smit", DateTimeOffset.UtcNow, "Herenknippen", "Kapsalon De Knip");

        Assert.Contains("Jan Smit", body, StringComparison.Ordinal);
    }

    [Fact]
    public void BookingConfirmation_BodyContainsServiceSummary()
    {
        var (_, body) = EmailTemplates.BookingConfirmation("Jan Smit", DateTimeOffset.UtcNow, "Herenknippen, Baard trimmen", "Kapsalon De Knip");

        Assert.Contains("Herenknippen, Baard trimmen", body, StringComparison.Ordinal);
    }

    [Fact]
    public void BookingConfirmation_BodyContainsSalonName()
    {
        var (_, body) = EmailTemplates.BookingConfirmation("Jan Smit", DateTimeOffset.UtcNow, "Herenknippen", "Kapsalon De Knip");

        Assert.Contains("Kapsalon De Knip", body, StringComparison.Ordinal);
    }

    [Fact]
    public void BookingReminder_ReturnsNonEmptySubjectAndBody()
    {
        var (subject, body) = EmailTemplates.BookingReminder("Jan Smit", DateTimeOffset.UtcNow, "Herenknippen", "Kapsalon De Knip");

        Assert.False(string.IsNullOrEmpty(subject));
        Assert.False(string.IsNullOrEmpty(body));
    }

    [Fact]
    public void BookingReminder_SubjectContainsHerinnering()
    {
        var (subject, _) = EmailTemplates.BookingReminder("Jan Smit", DateTimeOffset.UtcNow, "Herenknippen", "Kapsalon De Knip");

        Assert.Contains("Herinnering", subject, StringComparison.Ordinal);
    }

    [Fact]
    public void BookingReminder_BodyContainsClientName()
    {
        var (_, body) = EmailTemplates.BookingReminder("Jan Smit", DateTimeOffset.UtcNow, "Herenknippen", "Kapsalon De Knip");

        Assert.Contains("Jan Smit", body, StringComparison.Ordinal);
    }

    [Fact]
    public void BookingReminder_BodyContainsSalonName()
    {
        var (_, body) = EmailTemplates.BookingReminder("Jan Smit", DateTimeOffset.UtcNow, "Herenknippen", "Kapsalon De Knip");

        Assert.Contains("Kapsalon De Knip", body, StringComparison.Ordinal);
    }

    [Fact]
    public void BookingReceived_ReturnsNonEmptySubjectAndBody()
    {
        var (subject, body) = EmailTemplates.BookingReceived("Jan Smit", DateTimeOffset.UtcNow, "Herenknippen", "Kapsalon De Knip");

        Assert.False(string.IsNullOrEmpty(subject));
        Assert.False(string.IsNullOrEmpty(body));
    }

    [Fact]
    public void BookingReceived_SubjectContainsNieuweBoeking()
    {
        var (subject, _) = EmailTemplates.BookingReceived("Jan Smit", DateTimeOffset.UtcNow, "Herenknippen", "Kapsalon De Knip");

        Assert.Contains("Nieuwe boeking", subject, StringComparison.Ordinal);
    }

    [Fact]
    public void BookingReceived_BodyContainsClientName()
    {
        var (_, body) = EmailTemplates.BookingReceived("Jan Smit", DateTimeOffset.UtcNow, "Herenknippen", "Kapsalon De Knip");

        Assert.Contains("Jan Smit", body, StringComparison.Ordinal);
    }

    [Fact]
    public void BookingCancellation_ReturnsNonEmptySubjectAndBody()
    {
        var (subject, body) = EmailTemplates.BookingCancellation("Jan Smit", DateTimeOffset.UtcNow, "Kapsalon De Knip");

        Assert.False(string.IsNullOrEmpty(subject));
        Assert.False(string.IsNullOrEmpty(body));
    }

    [Fact]
    public void BookingCancellation_SubjectContainsGeannuleerd()
    {
        var (subject, _) = EmailTemplates.BookingCancellation("Jan Smit", DateTimeOffset.UtcNow, "Kapsalon De Knip");

        Assert.Contains("geannuleerd", subject, StringComparison.Ordinal);
    }

    [Fact]
    public void BookingCancellation_BodyContainsClientName()
    {
        var (_, body) = EmailTemplates.BookingCancellation("Jan Smit", DateTimeOffset.UtcNow, "Kapsalon De Knip");

        Assert.Contains("Jan Smit", body, StringComparison.Ordinal);
    }

    [Fact]
    public void BookingCancellation_BodyContainsSalonName()
    {
        var (_, body) = EmailTemplates.BookingCancellation("Jan Smit", DateTimeOffset.UtcNow, "Kapsalon De Knip");

        Assert.Contains("Kapsalon De Knip", body, StringComparison.Ordinal);
    }

    [Fact]
    public void InvoiceSent_ReturnsNonEmptySubjectAndBody()
    {
        var (subject, body) = EmailTemplates.InvoiceSent("Jan Smit", "2026-0001", DateOnly.FromDateTime(DateTime.UtcNow), 48.40m, "Kapsalon De Knip");

        Assert.False(string.IsNullOrEmpty(subject));
        Assert.False(string.IsNullOrEmpty(body));
    }

    [Fact]
    public void InvoiceSent_BodyContainsThankYouMessage()
    {
        var (_, body) = EmailTemplates.InvoiceSent("Jan Smit", "2026-0001", DateOnly.FromDateTime(DateTime.UtcNow), 48.40m, "Kapsalon De Knip");

        Assert.Contains("Bedankt voor uw bezoek", body, StringComparison.Ordinal);
    }

    [Fact]
    public void InvoiceSent_WhenPaid_ContainsPaidBadge()
    {
        var (_, body) = EmailTemplates.InvoiceSent("Jan Smit", "2026-0001", DateOnly.FromDateTime(DateTime.UtcNow), 48.40m, "Kapsalon De Knip", isPaid: true);

        Assert.Contains("reeds betaald", body, StringComparison.Ordinal);
    }

    [Fact]
    public void InvoiceSent_WhenNotPaid_DoesNotContainPaidBadge()
    {
        var (_, body) = EmailTemplates.InvoiceSent("Jan Smit", "2026-0001", DateOnly.FromDateTime(DateTime.UtcNow), 48.40m, "Kapsalon De Knip", isPaid: false);

        Assert.DoesNotContain("reeds betaald", body, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildTemplate_CustomServicesLabel_AppearsInHtml()
    {
        var html = EmailTemplates.BuildTemplate(
            "Salon", "Jan", "Message", "1 januari 2026", "Herenknippen", "Closing", servicesLabel: "Mijn diensten");

        Assert.Contains("Mijn diensten", html, StringComparison.Ordinal);
        Assert.DoesNotContain(">Diensten<", html, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildTemplate_DefaultServicesLabel_IsDiensten()
    {
        var html = EmailTemplates.BuildTemplate(
            "Salon", "Jan", "Message", "1 januari 2026", "Herenknippen", "Closing");

        Assert.Contains("Diensten", html, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildTemplate_CustomDateLabel_AppearsInHtml()
    {
        var html = EmailTemplates.BuildTemplate(
            "Salon", "Jan", "Message", "1 januari 2026", "Herenknippen", "Closing", dateLabel: "Aangepast label");

        Assert.Contains("Aangepast label", html, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildTemplateFromBody_ReturnsHtmlWithBodyContent()
    {
        var html = EmailTemplates.BuildTemplateFromBody("Mijn Salon", "Jan Smit", "<p>Uw afspraak is bevestigd.</p>");

        Assert.Contains("<!DOCTYPE html>", html, StringComparison.Ordinal);
        Assert.Contains("Mijn Salon", html, StringComparison.Ordinal);
        Assert.Contains("Beste Jan Smit", html, StringComparison.Ordinal);
        Assert.Contains("Uw afspraak is bevestigd.", html, StringComparison.Ordinal);
        Assert.Contains("Met vriendelijke groet", html, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildTemplateFromBody_DoesNotIncludeDateOrServiceSections()
    {
        var html = EmailTemplates.BuildTemplateFromBody("Salon", "Jan", "<p>Simple body</p>");

        // BuildTemplateFromBody should NOT contain the structured date/services table
        Assert.DoesNotContain("Datum en tijd", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Diensten", html, StringComparison.Ordinal);
    }
}
