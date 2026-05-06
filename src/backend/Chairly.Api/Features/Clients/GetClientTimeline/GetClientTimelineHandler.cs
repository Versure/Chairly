using Chairly.Api.Features.Billing;
using Chairly.Api.Features.Bookings;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Clients.GetClientTimeline;

internal sealed class GetClientTimelineHandler(ChairlyDbContext db, ITenantContext tenantContext) : IRequestHandler<GetClientTimelineQuery, OneOf<ClientTimelineResponse, NotFound>>
{
    public async Task<OneOf<ClientTimelineResponse, NotFound>> Handle(GetClientTimelineQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var tenantId = tenantContext.TenantId;

        var client = await db.Clients
            .FirstOrDefaultAsync(c => c.Id == query.ClientId && c.TenantId == tenantId && c.DeletedAtUtc == null, cancellationToken)
            .ConfigureAwait(false);

        if (client is null)
        {
            return new NotFound();
        }

        var profile = ToProfileResponse(client);

        var bookings = await db.Bookings
            .Include(b => b.BookingServices)
            .Where(b => b.ClientId == query.ClientId && b.TenantId == tenantId)
            .OrderByDescending(b => b.StartTime)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var staffLookup = await LoadStaffLookupAsync(bookings, tenantId, cancellationToken).ConfigureAwait(false);
        var recipeLookup = await LoadRecipeLookupAsync(query.ClientId, tenantId, bookings, staffLookup, cancellationToken).ConfigureAwait(false);
        var invoiceLookup = await LoadInvoiceLookupAsync(query.ClientId, tenantId, cancellationToken).ConfigureAwait(false);

        var timeline = BuildTimeline(bookings, staffLookup, recipeLookup, invoiceLookup);
        var stats = ComputeStats(bookings, staffLookup, invoiceLookup);

        return new ClientTimelineResponse(profile, stats, timeline);
    }

    private static ClientResponse ToProfileResponse(Client client) =>
        new(
            client.Id,
            client.FirstName,
            client.LastName,
            client.Email,
            client.PhoneNumber,
            client.Notes,
            client.CreatedAtUtc,
            client.UpdatedAtUtc);

    private async Task<Dictionary<Guid, string>> LoadStaffLookupAsync(
        List<Booking> bookings, Guid tenantId, CancellationToken cancellationToken)
    {
        var staffIds = bookings.Select(b => b.StaffMemberId).Distinct().ToList();
        var staffMembers = await db.StaffMembers
            .Where(s => staffIds.Contains(s.Id) && s.TenantId == tenantId)
            .Select(s => new { s.Id, FullName = s.FirstName + " " + s.LastName })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return staffMembers.ToDictionary(s => s.Id, s => s.FullName);
    }

    private async Task<Dictionary<Guid, ClientRecipeSummaryResponse>> LoadRecipeLookupAsync(
        Guid clientId, Guid tenantId, List<Booking> bookings,
        Dictionary<Guid, string> staffLookup, CancellationToken cancellationToken)
    {
        var recipes = await db.Recipes
            .Include(r => r.Products)
            .Where(r => r.ClientId == clientId && r.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return recipes.ToDictionary(
            r => r.BookingId,
            r => new ClientRecipeSummaryResponse(
                r.Id,
                r.BookingId,
                bookings.Find(b => b.Id == r.BookingId)?.StartTime ?? r.CreatedAtUtc,
                r.StaffMemberId,
                staffLookup.GetValueOrDefault(r.StaffMemberId, string.Empty),
                r.Title,
                r.Notes,
                r.Products.OrderBy(p => p.SortOrder).Select(p => new RecipeProductResponse(
                    p.Id, p.Name, p.Brand, p.Quantity, p.SortOrder)).ToList(),
                r.CreatedAtUtc,
                r.UpdatedAtUtc));
    }

    private async Task<Dictionary<Guid, ClientTimelineInvoiceResponse>> LoadInvoiceLookupAsync(
        Guid clientId, Guid tenantId, CancellationToken cancellationToken)
    {
        var invoices = await db.Invoices
            .Where(i => i.ClientId == clientId && i.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return invoices.ToDictionary(
            i => i.BookingId,
            i => new ClientTimelineInvoiceResponse(
                i.Id, i.InvoiceNumber, i.InvoiceDate, i.TotalAmount,
                InvoiceMapper.DeriveStatus(i), i.SentAtUtc, i.PaidAtUtc, i.VoidedAtUtc));
    }

    private static List<TimelineEntryResponse> BuildTimeline(
        List<Booking> bookings, Dictionary<Guid, string> staffLookup,
        Dictionary<Guid, ClientRecipeSummaryResponse> recipeLookup,
        Dictionary<Guid, ClientTimelineInvoiceResponse> invoiceLookup)
    {
        return bookings.Select(b => new TimelineEntryResponse(
            new BookingTimelineCardResponse(
                b.Id, b.StartTime, b.EndTime,
                (int)(b.EndTime - b.StartTime).TotalMinutes,
                BookingMapper.DeriveStatus(b),
                b.StaffMemberId,
                staffLookup.GetValueOrDefault(b.StaffMemberId, string.Empty),
                b.BookingServices.Sum(bs => bs.Price),
                b.Notes,
                b.BookingServices.OrderBy(bs => bs.SortOrder).Select(bs => new BookingTimelineServiceResponse(
                    bs.ServiceId, bs.ServiceName, (int)bs.Duration.TotalMinutes, bs.Price, bs.SortOrder)).ToList(),
                b.ConfirmedAtUtc, b.StartedAtUtc, b.CompletedAtUtc, b.CancelledAtUtc, b.NoShowAtUtc),
            recipeLookup.GetValueOrDefault(b.Id),
            invoiceLookup.GetValueOrDefault(b.Id))).ToList();
    }

    private static ClientTimelineStatsResponse ComputeStats(
        List<Booking> bookings, Dictionary<Guid, string> staffLookup,
        Dictionary<Guid, ClientTimelineInvoiceResponse> invoiceLookup)
    {
        var completedBookings = bookings.Where(b => b.CompletedAtUtc != null).ToList();

        var totalVisits = completedBookings.Count;

        var lastVisitAtUtc = completedBookings.Count > 0
            ? completedBookings.Max(b => b.StartTime)
            : (DateTimeOffset?)null;

        var totalSpentAmount = invoiceLookup.Values
            .Where(i => i.VoidedAtUtc == null)
            .Sum(i => i.TotalAmount);

        var noShowCount = bookings.Count(b => b.NoShowAtUtc != null);

        var mostVisitedStaffMember = ComputeMostVisitedStaffMember(completedBookings, staffLookup);
        var mostBookedService = ComputeMostBookedService(completedBookings);

        return new ClientTimelineStatsResponse(
            totalVisits, lastVisitAtUtc, totalSpentAmount,
            mostVisitedStaffMember, mostBookedService, noShowCount);
    }

    private static StaffMemberSummary? ComputeMostVisitedStaffMember(
        List<Booking> completedBookings, Dictionary<Guid, string> staffLookup)
    {
        if (completedBookings.Count == 0)
        {
            return null;
        }

        var topStaff = completedBookings
            .GroupBy(b => b.StaffMemberId)
            .Select(g => new { g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .First();

        return new StaffMemberSummary(
            topStaff.Key,
            staffLookup.GetValueOrDefault(topStaff.Key, string.Empty),
            topStaff.Count);
    }

    private static ServiceSummary? ComputeMostBookedService(List<Booking> completedBookings)
    {
        if (completedBookings.Count == 0)
        {
            return null;
        }

        var topService = completedBookings
            .SelectMany(b => b.BookingServices)
            .GroupBy(bs => bs.ServiceId)
            .Select(g => new { g.Key, Count = g.Count(), g.OrderByDescending(bs => bs.Booking?.StartTime ?? DateTimeOffset.MinValue).First().ServiceName })
            .OrderByDescending(x => x.Count)
            .First();

        return new ServiceSummary(topService.Key, topService.ServiceName, topService.Count);
    }
}
#pragma warning restore CA1812
