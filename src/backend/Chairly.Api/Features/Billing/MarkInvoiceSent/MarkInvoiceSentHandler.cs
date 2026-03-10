using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Billing.MarkInvoiceSent;

internal sealed class MarkInvoiceSentHandler(ChairlyDbContext db) : IRequestHandler<MarkInvoiceSentCommand, OneOf<InvoiceResponse, NotFound, Unprocessable>>
{
    public async Task<OneOf<InvoiceResponse, NotFound, Unprocessable>> Handle(MarkInvoiceSentCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var invoice = await db.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == command.Id && i.TenantId == TenantConstants.DefaultTenantId, cancellationToken)
            .ConfigureAwait(false);

        if (invoice is null)
        {
            return new NotFound();
        }

        if (invoice.VoidedAtUtc != null || invoice.PaidAtUtc != null)
        {
            return new Unprocessable("Factuur kan niet als verzonden worden gemarkeerd");
        }

        // Idempotent: if already sent, return current state
        if (invoice.SentAtUtc == null)
        {
            invoice.SentAtUtc = DateTimeOffset.UtcNow;
#pragma warning disable MA0026 // TODO: Replace with authenticated user ID from Keycloak (see Keycloak integration)
            invoice.SentBy = Guid.Empty;
#pragma warning restore MA0026

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var clientFullName = await db.Clients
            .Where(c => c.Id == invoice.ClientId)
            .Select(c => c.FirstName + " " + c.LastName)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? string.Empty;

        return InvoiceMapper.ToResponse(invoice, clientFullName);
    }
}
#pragma warning restore CA1812
