using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Domain.Entities;
using Chairly.Domain.Enums;
using Chairly.Domain.Events;
using Chairly.Infrastructure.Messaging;
using Chairly.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using OneOf;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Onboarding.CreateSubscription;

internal sealed partial class CreateSubscriptionHandler(
    WebsiteDbContext db,
    IOnboardingEventPublisher eventPublisher,
    ILogger<CreateSubscriptionHandler> logger) : IRequestHandler<CreateSubscriptionCommand, OneOf<SubscriptionResponse, Unprocessable>>
{
    private static readonly Dictionary<SubscriptionPlan, string> _planSlugs = new()
    {
        [SubscriptionPlan.Starter] = "starter",
        [SubscriptionPlan.Team] = "team",
        [SubscriptionPlan.Salon] = "salon",
    };

    public async Task<OneOf<SubscriptionResponse, Unprocessable>> Handle(CreateSubscriptionCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationResult = ValidateCommand(command);
        if (validationResult is not null)
        {
            return validationResult.Value;
        }

        var plan = Enum.Parse<SubscriptionPlan>(command.Plan, ignoreCase: true);
        var billingCycle = command.BillingCycle is not null
            ? Enum.Parse<BillingCycle>(command.BillingCycle, ignoreCase: true)
            : (BillingCycle?)null;

        var entity = CreateEntity(command, plan, billingCycle);

        db.Subscriptions.Add(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await TryPublishEventAsync(entity, cancellationToken).ConfigureAwait(false);

        return ToResponse(entity);
    }

    private static Unprocessable? ValidateCommand(CreateSubscriptionCommand command)
    {
        if (!Enum.TryParse<SubscriptionPlan>(command.Plan, ignoreCase: true, out _))
        {
            return new Unprocessable($"Ongeldig plan: '{command.Plan}'. Geldige waarden: starter, team, salon.");
        }

        if (command.BillingCycle is not null && !Enum.TryParse<BillingCycle>(command.BillingCycle, ignoreCase: true, out _))
        {
            return new Unprocessable($"Ongeldige factuurcyclus: '{command.BillingCycle}'. Geldige waarden: Monthly, Annual.");
        }

        var plan = Enum.Parse<SubscriptionPlan>(command.Plan, ignoreCase: true);
        var hasBillingCycle = command.BillingCycle is not null;

        if (command.IsTrial && plan != SubscriptionPlan.Starter)
        {
            return new Unprocessable("Proefperiode is alleen beschikbaar voor het Starter-plan.");
        }

        if (command.IsTrial && hasBillingCycle)
        {
            return new Unprocessable("Proefperiode mag geen factuurcyclus hebben.");
        }

        if (!command.IsTrial && !hasBillingCycle)
        {
            return new Unprocessable("Factuurcyclus is verplicht voor betaalde abonnementen.");
        }

        return null;
    }

    private static Subscription CreateEntity(CreateSubscriptionCommand command, SubscriptionPlan plan, BillingCycle? billingCycle) =>
        new()
        {
            Id = Guid.NewGuid(),
            SalonName = command.SalonName,
            OwnerFirstName = command.OwnerFirstName,
            OwnerLastName = command.OwnerLastName,
            Email = command.Email,
            PhoneNumber = command.PhoneNumber,
            Plan = plan,
            BillingCycle = command.IsTrial ? null : billingCycle,
            TrialEndsAtUtc = command.IsTrial ? DateTimeOffset.UtcNow.AddDays(30) : null,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = null,
        };

    private async Task TryPublishEventAsync(Subscription entity, CancellationToken cancellationToken)
    {
        try
        {
            await eventPublisher.PublishSubscriptionCreatedAsync(
                new SubscriptionCreatedEvent(
                    entity.Id,
                    entity.SalonName,
                    entity.OwnerFirstName,
                    entity.OwnerLastName,
                    entity.Email,
                    entity.PhoneNumber,
                    _planSlugs[entity.Plan],
                    entity.BillingCycle?.ToString(),
                    entity.IsTrial),
                cancellationToken).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Best-effort event publishing; subscription is already persisted
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogEventPublishFailed(logger, entity.Id, ex);
        }
    }

    internal static SubscriptionResponse ToResponse(Subscription entity) =>
        new(
            entity.Id,
            entity.SalonName,
            entity.OwnerFirstName,
            entity.OwnerLastName,
            entity.Email,
            _planSlugs[entity.Plan],
            entity.BillingCycle?.ToString(),
            entity.IsTrial,
            entity.TrialEndsAtUtc,
            entity.CreatedAtUtc);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to publish event for subscription {SubscriptionId}; notification may be delayed")]
    private static partial void LogEventPublishFailed(ILogger logger, Guid subscriptionId, Exception exception);
}
#pragma warning restore CA1812
