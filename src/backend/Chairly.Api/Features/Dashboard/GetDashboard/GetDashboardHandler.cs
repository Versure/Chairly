using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Dashboard.GetDashboard;

internal sealed class GetDashboardHandler(ChairlyDbContext db, ITenantContext tenantContext) : IRequestHandler<GetDashboardQuery, DashboardResponse>
{
    public async Task<DashboardResponse> Handle(GetDashboardQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var role = tenantContext.UserRole;
        var isStaffMember = string.Equals(role, "staff_member", StringComparison.Ordinal);
        var isOwner = string.Equals(role, "owner", StringComparison.Ordinal);
        var isManager = string.Equals(role, "manager", StringComparison.Ordinal);

        var currentStaffMemberId = await ResolveStaffMemberIdAsync(cancellationToken).ConfigureAwait(false);

        var todaysBookingResponses = await GetTodaysBookingsAsync(isStaffMember, currentStaffMemberId, cancellationToken).ConfigureAwait(false);
        var upcomingBookingResponses = await GetUpcomingBookingsAsync(isStaffMember, currentStaffMemberId, cancellationToken).ConfigureAwait(false);
        var newClientsThisWeek = isStaffMember ? 0 : await CountNewClientsThisWeekAsync(cancellationToken).ConfigureAwait(false);
        var canSeeRevenue = isOwner || isManager;
        var revenueThisWeek = canSeeRevenue ? await GetRevenueAsync(GetWeekStart(), cancellationToken).ConfigureAwait(false) : (decimal?)null;
        var revenueThisMonth = canSeeRevenue ? await GetRevenueAsync(GetMonthStart(), cancellationToken).ConfigureAwait(false) : (decimal?)null;

        return new DashboardResponse(
            todaysBookingResponses.Count,
            todaysBookingResponses,
            upcomingBookingResponses,
            newClientsThisWeek,
            revenueThisWeek,
            revenueThisMonth);
    }

    private async Task<Guid?> ResolveStaffMemberIdAsync(CancellationToken cancellationToken)
    {
        var userIdString = tenantContext.UserId.ToString();
        return await db.StaffMembers
            .Where(s => s.TenantId == tenantContext.TenantId && s.KeycloakUserId == userIdString)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<List<DashboardBookingResponse>> GetTodaysBookingsAsync(
        bool isStaffMember, Guid? currentStaffMemberId, CancellationToken cancellationToken)
    {
        var todayStart = new DateTimeOffset(DateTimeOffset.UtcNow.Date, TimeSpan.Zero);
        var todayEnd = todayStart.AddDays(1);

        var query = BuildBookingQuery()
            .Where(b => b.StartTime >= todayStart && b.StartTime < todayEnd);

        if (isStaffMember && currentStaffMemberId.HasValue)
        {
            query = query.Where(b => b.StaffMemberId == currentStaffMemberId.Value);
        }

        var bookings = await query.OrderBy(b => b.StartTime)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return bookings.Select(ToBookingResponse).ToList();
    }

    private async Task<List<DashboardBookingResponse>> GetUpcomingBookingsAsync(
        bool isStaffMember, Guid? currentStaffMemberId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var query = BuildBookingQuery()
            .Where(b => b.StartTime > now)
            .Where(b => b.CancelledAtUtc == null)
            .Where(b => b.NoShowAtUtc == null)
            .Where(b => b.CompletedAtUtc == null);

        if (isStaffMember && currentStaffMemberId.HasValue)
        {
            query = query.Where(b => b.StaffMemberId == currentStaffMemberId.Value);
        }

        var bookings = await query.OrderBy(b => b.StartTime).Take(5)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return bookings.Select(ToBookingResponse).ToList();
    }

    private async Task<int> CountNewClientsThisWeekAsync(CancellationToken cancellationToken)
    {
        var weekStart = GetWeekStart();
        return await db.Clients
            .Where(c => c.TenantId == tenantContext.TenantId)
            .Where(c => c.CreatedAtUtc >= weekStart)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<decimal> GetRevenueAsync(DateTimeOffset from, CancellationToken cancellationToken)
    {
        return await db.Invoices
            .Where(i => i.TenantId == tenantContext.TenantId)
            .Where(i => i.PaidAtUtc != null)
            .Where(i => i.VoidedAtUtc == null)
            .Where(i => i.PaidAtUtc >= from)
            .SumAsync(i => i.TotalAmount, cancellationToken)
            .ConfigureAwait(false);
    }

    private IQueryable<Booking> BuildBookingQuery()
    {
        return db.Bookings
            .Include(b => b.BookingServices)
            .Include(b => b.Client)
            .Include(b => b.StaffMember)
            .Where(b => b.TenantId == tenantContext.TenantId);
    }

    private static DashboardBookingResponse ToBookingResponse(Booking b)
    {
        return new DashboardBookingResponse(
            b.Id,
            b.ClientId,
            b.Client != null ? b.Client.FirstName + " " + b.Client.LastName : string.Empty,
            b.StaffMemberId,
            b.StaffMember != null ? b.StaffMember.FirstName + " " + b.StaffMember.LastName : string.Empty,
            b.StartTime,
            b.EndTime,
            b.DeriveStatus().ToString(),
            b.BookingServices.OrderBy(bs => bs.SortOrder).Select(bs => bs.ServiceName).ToList());
    }

    private static DateTimeOffset GetWeekStart()
    {
        var today = DateTimeOffset.UtcNow.Date;
        var diff = (7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        return new DateTimeOffset(today.AddDays(-diff), TimeSpan.Zero);
    }

    private static DateTimeOffset GetMonthStart()
    {
        return new DateTimeOffset(
            new DateTime(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            TimeSpan.Zero);
    }
}
#pragma warning restore CA1812
