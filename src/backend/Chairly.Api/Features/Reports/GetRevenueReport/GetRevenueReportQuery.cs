using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;

namespace Chairly.Api.Features.Reports.GetRevenueReport;

internal sealed record GetRevenueReportQuery(
    [property: Required] string Period,
    DateOnly Date)
    : IRequest<OneOf<RevenueReportResponse, Unprocessable>>;
