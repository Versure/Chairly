using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Domain.Enums;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Billing.MarkInvoicePaid;

internal sealed record MarkInvoicePaidCommand(Guid Id, [property: Required][property: EnumDataType(typeof(PaymentMethod))] PaymentMethod PaymentMethod) : IRequest<OneOf<InvoiceResponse, NotFound, Unprocessable>>;
