using System.Security.Claims;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Events;
using Chairly.Infrastructure.Messaging;
using Chairly.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Newsletters.TestSendNewsletter;

internal sealed class TestSendNewsletterHandler(
    ChairlyDbContext db,
    INewsletterEventPublisher eventPublisher,
    ITenantContext tenantContext,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<TestSendNewsletterCommand, OneOf<Success, NotFound, Unprocessable>>
{
    public async Task<OneOf<Success, NotFound, Unprocessable>> Handle(TestSendNewsletterCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var campaign = await db.NewsletterCampaigns
            .FirstOrDefaultAsync(c => c.Id == command.Id && c.TenantId == tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (campaign is null)
        {
            return new NotFound();
        }

        var email = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email)
            ?? httpContextAccessor.HttpContext?.User?.FindFirstValue("email");

        if (string.IsNullOrWhiteSpace(email))
        {
            return new Unprocessable("Geen e-mailadres gevonden voor de huidige gebruiker.");
        }

        await eventPublisher.PublishTestRequestedAsync(
            new NewsletterTestRequestedEvent(campaign.TenantId, campaign.Id, email, tenantContext.UserId),
            cancellationToken).ConfigureAwait(false);

        return new Success();
    }
}
#pragma warning restore CA1812
