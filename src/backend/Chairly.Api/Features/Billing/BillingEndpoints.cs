using Chairly.Api.Features.Billing.AddInvoiceLineItem;
using Chairly.Api.Features.Billing.GenerateInvoice;
using Chairly.Api.Features.Billing.GetInvoice;
using Chairly.Api.Features.Billing.GetInvoicesList;
using Chairly.Api.Features.Billing.MarkInvoicePaid;
using Chairly.Api.Features.Billing.MarkInvoiceSent;
using Chairly.Api.Features.Billing.RegenerateInvoice;
using Chairly.Api.Features.Billing.RemoveInvoiceLineItem;
using Chairly.Api.Features.Billing.VoidInvoice;

namespace Chairly.Api.Features.Billing;

internal static class BillingEndpoints
{
    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/invoices")
            .RequireAuthorization("RequireManager");

        group.MapGenerateInvoice();
        group.MapGetInvoicesList();
        group.MapGetInvoice();
        group.MapMarkInvoiceSent();
        group.MapMarkInvoicePaid();
        group.MapVoidInvoice();
        group.MapRegenerateInvoice();
        group.MapAddInvoiceLineItem();
        group.MapRemoveInvoiceLineItem();

        return app;
    }
}
