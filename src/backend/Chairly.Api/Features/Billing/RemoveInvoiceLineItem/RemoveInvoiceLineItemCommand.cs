using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Billing.RemoveInvoiceLineItem;

internal sealed record RemoveInvoiceLineItemCommand(Guid InvoiceId, Guid LineItemId) : IRequest<OneOf<InvoiceResponse, NotFound, Unprocessable>>;
