using Chairly.Api.Shared.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Chairly.Api.Features.Reports.GetRevenueReport;

internal static class GetRevenueReportEndpoint
{
    public static void MapGetRevenueReport(this RouteGroupBuilder group)
    {
        group.MapGet("/revenue", async (
            [FromQuery] string period,
            [FromQuery] DateOnly date,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(
                new GetRevenueReportQuery(period, date), cancellationToken).ConfigureAwait(false);
            return result.Match(
                report => Results.Ok(report),
                unprocessable => Results.UnprocessableEntity(new { message = unprocessable.Message }));
        });
    }
}
