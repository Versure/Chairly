using System.Security.Cryptography;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Api.Features.Newsletters;

internal static class NewsletterRecipientLoader
{
    internal sealed record Recipient(Guid Id, string Email);

    public static async Task<IReadOnlyList<Recipient>> LoadAsync(ChairlyDbContext db, Guid tenantId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);
        return await db.Clients
            .Where(c => c.TenantId == tenantId
                && c.IsSubscribedToNewsletter
                && c.DeletedAtUtc == null
                && c.Email != null
                && c.Email != string.Empty)
            .Select(c => new Recipient(c.Id, c.Email!))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public static void AddDeliveries(ChairlyDbContext db, NewsletterCampaign campaign, IReadOnlyList<Recipient> recipients, DateTimeOffset now, Guid createdBy)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(recipients);
        foreach (var recipient in recipients)
        {
            db.NewsletterDeliveries.Add(new NewsletterDelivery
            {
                Id = Guid.NewGuid(),
                TenantId = campaign.TenantId,
                CampaignId = campaign.Id,
                ClientId = recipient.Id,
                Email = recipient.Email,
                UnsubscribeToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
                CreatedAtUtc = now,
                CreatedBy = createdBy,
            });
        }
    }
}
