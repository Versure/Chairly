using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Billing.VoidInvoice;

internal sealed record VoidInvoiceCommand(Guid Id) : IRequest<OneOf<InvoiceResponse, NotFound, Unprocessable>>;
