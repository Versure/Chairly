using Chairly.Api.Features.Clients.GetClientTimeline;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;

namespace Chairly.Tests.Features.Clients;

public class GetClientTimelineEndpointTests
{
    private static ChairlyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ChairlyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ChairlyDbContext(options);
    }

    private static Client CreateTestClient(
        ChairlyDbContext db,
        Guid? tenantId = null,
        string firstName = "Anna",
        string lastName = "Bakker")
    {
        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId ?? TestTenantContext.DefaultTenantId,
            FirstName = firstName,
            LastName = lastName,
            Email = "anna@example.com",
            PhoneNumber = "0612345678",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
        };
        db.Clients.Add(client);
        db.SaveChanges();
        return client;
    }

    private static StaffMember CreateTestStaffMember(
        ChairlyDbContext db,
        string firstName = "Pieter",
        string lastName = "de Vries",
        Guid? id = null,
        Guid? tenantId = null)
    {
        var staffMember = new StaffMember
        {
            Id = id ?? Guid.NewGuid(),
            TenantId = tenantId ?? TestTenantContext.DefaultTenantId,
            FirstName = firstName,
            LastName = lastName,
            Color = "#FF5733",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
        };
        db.StaffMembers.Add(staffMember);
        db.SaveChanges();
        return staffMember;
    }

    private static Booking CreateTestBooking(
        ChairlyDbContext db,
        Guid clientId,
        Guid staffMemberId,
        DateTimeOffset startTime,
        bool completed = false,
        Guid? tenantId = null)
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId ?? TestTenantContext.DefaultTenantId,
            ClientId = clientId,
            StaffMemberId = staffMemberId,
            StartTime = startTime,
            EndTime = startTime.AddHours(1),
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-7),
            CreatedBy = Guid.Empty,
            CompletedAtUtc = completed ? startTime.AddMinutes(55) : null,
            CompletedBy = completed ? Guid.Empty : null,
            BookingServices =
            [
                new BookingService
                {
                    Id = Guid.NewGuid(),
                    ServiceId = Guid.NewGuid(),
                    ServiceName = "Knippen",
                    Duration = TimeSpan.FromMinutes(30),
                    Price = 35.00m,
                    SortOrder = 0,
                },
            ],
        };
        db.Bookings.Add(booking);
        db.SaveChanges();
        return booking;
    }

    /// <summary>
    /// Scenario 1: Returns 200 with the wrapped payload for a known client.
    /// </summary>
    [Fact]
    public async Task Handle_KnownClient_ReturnsTimelineResponse()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        CreateTestBooking(db, client.Id, staff.Id, DateTimeOffset.UtcNow.AddDays(-3), completed: true);

        var handler = new GetClientTimelineHandler(db, TestTenantContext.Create());
        var result = await handler.Handle(new GetClientTimelineQuery(client.Id));

        Assert.True(result.IsT0);
        var response = result.AsT0;
        Assert.Equal(client.Id, response.Profile.Id);
        Assert.Equal("Anna", response.Profile.FirstName);
        Assert.Equal("Bakker", response.Profile.LastName);
        Assert.Single(response.Timeline);
        Assert.Equal(1, response.Stats.TotalVisits);
    }

    /// <summary>
    /// Scenario 2: Returns NotFound for an unknown client id.
    /// </summary>
    [Fact]
    public async Task Handle_UnknownClientId_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new GetClientTimelineHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new GetClientTimelineQuery(Guid.NewGuid()));

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }

    /// <summary>
    /// Scenario 3: Staff member can view a client whose booking history contains bookings
    /// performed by other staff members (regression guard for Decision 9: no staff-scoping).
    /// </summary>
    [Fact]
    public async Task Handle_StaffMemberRole_CanSeeBookingsPerformedByOtherStaff()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);

        // The requesting staff member
        var requestingStaff = CreateTestStaffMember(db, "Sara", "Jansen");
        // A different staff member who performed the bookings
        var otherStaff = CreateTestStaffMember(db, "Pieter", "de Vries");

        // Bookings performed by the requesting staff member
        CreateTestBooking(db, client.Id, requestingStaff.Id, DateTimeOffset.UtcNow.AddDays(-5), completed: true);
        // Bookings performed by the other staff member
        var otherBooking1 = CreateTestBooking(db, client.Id, otherStaff.Id, DateTimeOffset.UtcNow.AddDays(-3), completed: true);
        var otherBooking2 = CreateTestBooking(db, client.Id, otherStaff.Id, DateTimeOffset.UtcNow.AddDays(-1), completed: true);

        // Authenticate as staff_member role (the requesting staff)
        var tenantContext = new TestTenantContext
        {
            TenantId = TestTenantContext.DefaultTenantId,
            UserId = requestingStaff.Id,
            UserRole = "staff_member",
        };

        var handler = new GetClientTimelineHandler(db, tenantContext);
        var result = await handler.Handle(new GetClientTimelineQuery(client.Id));

        Assert.True(result.IsT0);
        var response = result.AsT0;
        Assert.Equal(3, response.Timeline.Count);

        // Verify the timeline includes bookings from the other staff member
        var otherStaffBookingIds = new[] { otherBooking1.Id, otherBooking2.Id };
        var timelineBookingIds = response.Timeline.Select(e => e.Booking.Id).ToList();
        foreach (var bookingId in otherStaffBookingIds)
        {
            Assert.Contains(bookingId, timelineBookingIds);
        }

        // Verify the other staff member name is present
        var staffNames = response.Timeline.Select(e => e.Booking.StaffMemberName).Distinct(StringComparer.Ordinal).ToList();
        Assert.Contains("Pieter de Vries", staffNames);
        Assert.Contains("Sara Jansen", staffNames);
    }

    /// <summary>
    /// Scenario 4: Tenant isolation — a client in tenant A is not retrievable by a user in tenant B.
    /// </summary>
    [Fact]
    public async Task Handle_ClientInDifferentTenant_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // Client belongs to tenant A
        var client = CreateTestClient(db, tenantId: tenantA);
        var staff = CreateTestStaffMember(db, tenantId: tenantA);
        CreateTestBooking(db, client.Id, staff.Id, DateTimeOffset.UtcNow.AddDays(-3), completed: true, tenantId: tenantA);

        // User is in tenant B
        var tenantContext = new TestTenantContext
        {
            TenantId = tenantB,
            UserId = Guid.NewGuid(),
            UserRole = "owner",
        };

        var handler = new GetClientTimelineHandler(db, tenantContext);
        var result = await handler.Handle(new GetClientTimelineQuery(client.Id));

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }
}
