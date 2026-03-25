using Chairly.Api.Features.Reports.GetRevenueReport;
using Chairly.Api.Features.Reports.GetRevenueReportPdf;

namespace Chairly.Api.Features.Reports;

internal static class ReportsEndpoints
{
    public static IEndpointRouteBuilder MapReportsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports")
            .RequireAuthorization("RequireManager");

        group.MapGetRevenueReport();
        group.MapGetRevenueReportPdf();

        return app;
    }
}
