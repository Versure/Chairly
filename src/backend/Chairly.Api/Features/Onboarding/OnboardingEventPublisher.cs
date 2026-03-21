using Chairly.Api.Features.Notifications.Infrastructure;
using Chairly.Domain.Events;
using Chairly.Infrastructure.Messaging;
using Microsoft.Extensions.Options;

namespace Chairly.Api.Features.Onboarding;

#pragma warning disable CA1812
internal sealed class OnboardingEventPublisher(
    IEmailSender emailSender,
    IOptions<OnboardingSettings> onboardingSettings) : IOnboardingEventPublisher
{
    public async Task PublishDemoRequestSubmittedAsync(DemoRequestSubmittedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var adminEmail = onboardingSettings.Value.AdminEmail;
        var htmlBody = $"""
            <h2>Nieuwe demo-aanvraag</h2>
            <p><strong>Contactpersoon:</strong> {domainEvent.ContactName}</p>
            <p><strong>Salonnaam:</strong> {domainEvent.SalonName}</p>
            <p><strong>E-mail:</strong> {domainEvent.Email}</p>
            <p><strong>Telefoon:</strong> {domainEvent.PhoneNumber ?? "-"}</p>
            <p><strong>Bericht:</strong> {domainEvent.Message ?? "-"}</p>
            """;

        await emailSender.SendAsync(
            adminEmail,
            "Chairly Admin",
            $"Nieuwe demo-aanvraag: {domainEvent.SalonName}",
            htmlBody,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task PublishSignUpRequestSubmittedAsync(SignUpRequestSubmittedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var adminEmail = onboardingSettings.Value.AdminEmail;
        var htmlBody = $"""
            <h2>Nieuwe aanmelding</h2>
            <p><strong>Salonnaam:</strong> {domainEvent.SalonName}</p>
            <p><strong>Eigenaar:</strong> {domainEvent.OwnerFirstName} {domainEvent.OwnerLastName}</p>
            <p><strong>E-mail:</strong> {domainEvent.Email}</p>
            <p><strong>Telefoon:</strong> {domainEvent.PhoneNumber ?? "-"}</p>
            """;

        await emailSender.SendAsync(
            adminEmail,
            "Chairly Admin",
            $"Nieuwe aanmelding: {domainEvent.SalonName}",
            htmlBody,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
#pragma warning restore CA1812
