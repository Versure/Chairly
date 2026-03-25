using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Infrastructure.Persistence;
using OneOf;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Reports.GetRevenueReport;

internal sealed class GetRevenueReportHandler(ChairlyDbContext db, ITenantContext tenantContext) : IRequestHandler<GetRevenueReportQuery, OneOf<RevenueReportResponse, Unprocessable>>
{
    public async Task<OneOf<RevenueReportResponse, Unprocessable>> Handle(GetRevenueReportQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var periodResult = RevenueReportBuilder.CalculatePeriod(query.Period, query.Date);

        if (periodResult.IsT1)
        {
            return periodResult.AsT1;
        }

        var (periodStart, periodEnd) = periodResult.AsT0;

        var report = await RevenueReportBuilder
            .BuildReportAsync(db, tenantContext, query.Period, periodStart, periodEnd, cancellationToken)
            .ConfigureAwait(false);

        return report;
    }
}
#pragma warning restore CA1812
