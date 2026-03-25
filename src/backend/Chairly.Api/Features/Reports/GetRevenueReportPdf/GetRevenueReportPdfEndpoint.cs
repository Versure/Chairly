using Chairly.Api.Shared.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Chairly.Api.Features.Reports.GetRevenueReportPdf;

internal static class GetRevenueReportPdfEndpoint
{
    public static void MapGetRevenueReportPdf(this RouteGroupBuilder group)
    {
        group.MapGet("/revenue/pdf", async (
            [FromQuery] string period,
            [FromQuery] DateOnly date,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(
                new GetRevenueReportPdfQuery(period, date), cancellationToken).ConfigureAwait(false);
            return result.Match(
                pdf => Results.File(pdf, "application/pdf", $"omzetrapport-{period}-{date:yyyy-MM-dd}.pdf"),
                unprocessable => Results.UnprocessableEntity(new { message = unprocessable.Message }));
        });
    }
}
