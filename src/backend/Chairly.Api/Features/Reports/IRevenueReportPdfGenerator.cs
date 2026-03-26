namespace Chairly.Api.Features.Reports;

internal interface IRevenueReportPdfGenerator
{
    byte[] Generate(RevenueReportResponse data);
}
