using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Admin.CancelSubscription;

internal sealed class CancelSubscriptionCommand : IRequest<OneOf<AdminSubscriptionDetailResponse, NotFound, Unprocessable>>
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(1000)]
    public string CancellationReason { get; set; } = string.Empty;
}
#pragma warning restore CA1812
