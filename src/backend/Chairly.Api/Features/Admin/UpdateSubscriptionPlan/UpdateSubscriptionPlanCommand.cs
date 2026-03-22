using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Admin.UpdateSubscriptionPlan;

internal sealed class UpdateSubscriptionPlanCommand : IRequest<OneOf<AdminSubscriptionDetailResponse, NotFound, Unprocessable>>
{
    public Guid Id { get; set; }

    [Required]
    public string Plan { get; set; } = string.Empty;

    public string? BillingCycle { get; set; }
}
#pragma warning restore CA1812
