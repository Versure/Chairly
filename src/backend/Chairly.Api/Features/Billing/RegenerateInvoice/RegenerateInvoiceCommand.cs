using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Billing.RegenerateInvoice;

internal sealed record RegenerateInvoiceCommand(Guid Id) : IRequest<OneOf<InvoiceResponse, NotFound, Unprocessable>>;
