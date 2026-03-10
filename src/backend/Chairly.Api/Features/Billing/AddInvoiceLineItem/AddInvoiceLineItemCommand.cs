using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812 // Instantiated via ASP.NET Core model binding
namespace Chairly.Api.Features.Billing.AddInvoiceLineItem;

internal sealed class AddInvoiceLineItemCommand : IRequest<OneOf<InvoiceResponse, NotFound, Unprocessable>>
{
    [Required]
    public Guid InvoiceId { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    public decimal UnitPrice { get; set; }

    [Range(0, 100)]
    public decimal VatPercentage { get; set; } = 21.00m;
}
#pragma warning restore CA1812
