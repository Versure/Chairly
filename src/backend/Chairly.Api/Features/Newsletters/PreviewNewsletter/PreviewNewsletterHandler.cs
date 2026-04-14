using Chairly.Api.Features.Newsletters.Infrastructure;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Newsletters.PreviewNewsletter;

internal sealed class PreviewNewsletterHandler(
    ChairlyDbContext db,
    INewsletterHtmlSanitizer sanitizer,
    ITenantContext tenantContext) : IRequestHandler<PreviewNewsletterCommand, PreviewNewsletterResponse>
{
    public async Task<PreviewNewsletterResponse> Handle(PreviewNewsletterCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sanitised = sanitizer.Sanitize(command.BodyHtml);

        var settings = await db.TenantSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantContext.TenantId, cancellationToken)
            .ConfigureAwait(false);
        var salonName = settings?.CompanyName ?? "Uw salon";

        var html = NewsletterRenderer.Render(sanitised, salonName, "#preview-unsubscribe");
        return new PreviewNewsletterResponse(command.Subject, html);
    }
}
#pragma warning restore CA1812
