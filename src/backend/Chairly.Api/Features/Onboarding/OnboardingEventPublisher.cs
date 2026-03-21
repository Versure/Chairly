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
    public async Task PublishSubscriptionCreatedAsync(SubscriptionCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var adminEmail = onboardingSettings.Value.AdminEmail;
        var subject = domainEvent.IsTrial
            ? $"Nieuwe proefperiode: {domainEvent.SalonName}"
            : $"Nieuw abonnement: {domainEvent.SalonName}";

        var subscriptionType = domainEvent.IsTrial ? "Proefperiode" : "Betaald abonnement";
        var billingCycleText = domainEvent.BillingCycle ?? "-";

        var htmlBody = $"""
            <h2>{subject}</h2>
            <p><strong>Type:</strong> {subscriptionType}</p>
            <p><strong>Plan:</strong> {domainEvent.Plan}</p>
            <p><strong>Factuurcyclus:</strong> {billingCycleText}</p>
            <p><strong>Salonnaam:</strong> {domainEvent.SalonName}</p>
            <p><strong>Eigenaar:</strong> {domainEvent.OwnerFirstName} {domainEvent.OwnerLastName}</p>
            <p><strong>E-mail:</strong> {domainEvent.Email}</p>
            <p><strong>Telefoon:</strong> {domainEvent.PhoneNumber ?? "-"}</p>
            """;

        await emailSender.SendAsync(
            adminEmail,
            "Chairly Admin",
            subject,
            htmlBody,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
#pragma warning restore CA1812
