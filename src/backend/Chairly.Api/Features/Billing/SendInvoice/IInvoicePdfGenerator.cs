namespace Chairly.Api.Features.Billing.SendInvoice;

internal interface IInvoicePdfGenerator
{
    byte[] Generate(InvoicePdfData data);
}
