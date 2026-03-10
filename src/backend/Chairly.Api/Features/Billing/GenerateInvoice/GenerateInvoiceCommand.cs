using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812 // Instantiated via ASP.NET Core model binding
namespace Chairly.Api.Features.Billing.GenerateInvoice;

internal sealed class GenerateInvoiceCommand : IRequest<OneOf<InvoiceResponse, NotFound, Unprocessable, Conflict>>
{
    [Required]
    public Guid BookingId { get; set; }
}
#pragma warning restore CA1812
