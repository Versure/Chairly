using System.ComponentModel.DataAnnotations;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;

namespace Chairly.Api.Features.Reports.GetRevenueReportPdf;

internal sealed record GetRevenueReportPdfQuery(
    [property: Required] string Period,
    DateOnly Date)
    : IRequest<OneOf<byte[], Unprocessable>>;
