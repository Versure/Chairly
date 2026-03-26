using Chairly.Domain.Enums;

namespace Chairly.Api.Features.Billing.MarkInvoicePaid;

internal sealed record MarkInvoicePaidRequest(PaymentMethod PaymentMethod);
