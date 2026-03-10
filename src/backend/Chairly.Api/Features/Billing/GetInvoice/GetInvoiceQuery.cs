using Chairly.Api.Shared.Mediator;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Billing.GetInvoice;

internal sealed class GetInvoiceQuery(Guid id) : IRequest<OneOf<InvoiceResponse, NotFound>>
{
    public Guid Id { get; } = id;
}
