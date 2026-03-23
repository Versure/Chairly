namespace Chairly.Api.Features.Dashboard;

internal sealed record DashboardResponse(
    int TodaysBookingsCount,
    IReadOnlyList<DashboardBookingResponse> TodaysBookings,
    IReadOnlyList<DashboardBookingResponse> UpcomingBookings,
    int NewClientsThisWeek,
    decimal? RevenueThisWeek,
    decimal? RevenueThisMonth);
