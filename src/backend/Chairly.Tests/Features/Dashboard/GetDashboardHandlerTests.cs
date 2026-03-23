using Chairly.Api.Features.Dashboard.GetDashboard;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Tests.Features.Dashboard;

public class GetDashboardHandlerTests
{
    private static ChairlyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ChairlyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ChairlyDbContext(options);
    }

    private static StaffMember CreateTestStaffMember(
        ChairlyDbContext db,
        string? keycloakUserId = null,
        string firstName = "Test",
        string lastName = "Staff")
    {
        var staffMember = new StaffMember
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            FirstName = firstName,
            LastName = lastName,
            Color = "#000000",
            KeycloakUserId = keycloakUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.StaffMembers.Add(staffMember);
        db.SaveChanges();
        return staffMember;
    }

    private static Client CreateTestClient(
        ChairlyDbContext db,
        DateTimeOffset? createdAtUtc = null,
        string firstName = "Test",
        string lastName = "Client")
    {
        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            FirstName = firstName,
            LastName = lastName,
            CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow,
        };
        db.Clients.Add(client);
        db.SaveChanges();
        return client;
    }

    private static Booking CreateTestBooking(
        ChairlyDbContext db,
        Guid clientId,
        Guid staffMemberId,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null,
        string serviceName = "Test Service",
        bool cancelled = false,
        bool noShow = false,
        bool completed = false)
    {
        var start = startTime ?? DateTimeOffset.UtcNow.AddHours(1);
        var end = endTime ?? start.AddMinutes(30);
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            ClientId = clientId,
            StaffMemberId = staffMemberId,
            StartTime = start,
            EndTime = end,
            CancelledAtUtc = cancelled ? DateTimeOffset.UtcNow : null,
            NoShowAtUtc = noShow ? DateTimeOffset.UtcNow : null,
            CompletedAtUtc = completed ? DateTimeOffset.UtcNow : null,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        booking.BookingServices.Add(new BookingService
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            ServiceId = Guid.NewGuid(),
            ServiceName = serviceName,
            Duration = TimeSpan.FromMinutes(30),
            Price = 25.00m,
            SortOrder = 0,
        });

        db.Bookings.Add(booking);
        db.SaveChanges();
        return booking;
    }

    private static Invoice CreateTestInvoice(
        ChairlyDbContext db,
        decimal totalAmount,
        DateTimeOffset? paidAtUtc = null,
        DateTimeOffset? voidedAtUtc = null)
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            BookingId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            InvoiceNumber = "INV-001",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            SubTotalAmount = totalAmount,
            TotalVatAmount = 0m,
            TotalAmount = totalAmount,
            PaidAtUtc = paidAtUtc,
            VoidedAtUtc = voidedAtUtc,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Invoices.Add(invoice);
        db.SaveChanges();
        return invoice;
    }

    private static DateTimeOffset TodayAt(int hour, int minute = 0)
    {
        var today = DateTimeOffset.UtcNow.Date;
        return new DateTimeOffset(today.AddHours(hour).AddMinutes(minute), TimeSpan.Zero);
    }

    private static DateTimeOffset WeekStart()
    {
        var today = DateTimeOffset.UtcNow.Date;
        var diff = (7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        return new DateTimeOffset(today.AddDays(-diff), TimeSpan.Zero);
    }

    // ==================== Today's bookings ====================

    [Fact]
    public async Task Handle_ReturnsTodaysBookings_WithClientAndStaffNames()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        var staff = CreateTestStaffMember(db, tenantContext.UserId.ToString());
        var client = CreateTestClient(db, firstName: "Alice", lastName: "Wonder");
        CreateTestBooking(db, client.Id, staff.Id, TodayAt(10), TodayAt(10, 30), "Haircut");

        var handler = new GetDashboardHandler(db, tenantContext);
        var result = await handler.Handle(new GetDashboardQuery());

        Assert.Equal(1, result.TodaysBookingsCount);
        Assert.Single(result.TodaysBookings);
        var booking = result.TodaysBookings[0];
        Assert.Equal("Alice Wonder", booking.ClientName);
        Assert.Equal("Test Staff", booking.StaffMemberName);
        Assert.Contains("Haircut", booking.ServiceNames);
    }

    [Fact]
    public async Task Handle_StaffMemberRole_SeesOnlyOwnBookingsToday()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        tenantContext.UserRole = "staff_member";

        var ownStaff = CreateTestStaffMember(db, tenantContext.UserId.ToString(), "Own", "Staff");
        var otherStaff = CreateTestStaffMember(db, Guid.NewGuid().ToString(), "Other", "Staff");
        var client = CreateTestClient(db);

        CreateTestBooking(db, client.Id, ownStaff.Id, TodayAt(10), TodayAt(10, 30));
        CreateTestBooking(db, client.Id, otherStaff.Id, TodayAt(11), TodayAt(11, 30));

        var handler = new GetDashboardHandler(db, tenantContext);
        var result = await handler.Handle(new GetDashboardQuery());

        Assert.Equal(1, result.TodaysBookingsCount);
        Assert.Single(result.TodaysBookings);
        Assert.Equal(ownStaff.Id, result.TodaysBookings[0].StaffMemberId);
    }

    [Fact]
    public async Task Handle_OwnerRole_SeesAllBookingsToday()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        tenantContext.UserRole = "owner";

        var staff1 = CreateTestStaffMember(db, tenantContext.UserId.ToString());
        var staff2 = CreateTestStaffMember(db, Guid.NewGuid().ToString(), "Other", "Staff");
        var client = CreateTestClient(db);

        CreateTestBooking(db, client.Id, staff1.Id, TodayAt(10), TodayAt(10, 30));
        CreateTestBooking(db, client.Id, staff2.Id, TodayAt(11), TodayAt(11, 30));

        var handler = new GetDashboardHandler(db, tenantContext);
        var result = await handler.Handle(new GetDashboardQuery());

        Assert.Equal(2, result.TodaysBookingsCount);
        Assert.Equal(2, result.TodaysBookings.Count);
    }

    [Fact]
    public async Task Handle_NoBookingsToday_ReturnsEmptyList()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        CreateTestStaffMember(db, tenantContext.UserId.ToString());

        var handler = new GetDashboardHandler(db, tenantContext);
        var result = await handler.Handle(new GetDashboardQuery());

        Assert.Equal(0, result.TodaysBookingsCount);
        Assert.Empty(result.TodaysBookings);
    }

    // ==================== Upcoming bookings ====================

    [Fact]
    public async Task Handle_UpcomingBookings_ReturnsMax5OrderedByStartTime()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        var staff = CreateTestStaffMember(db, tenantContext.UserId.ToString());
        var client = CreateTestClient(db);

        for (var i = 1; i <= 7; i++)
        {
            CreateTestBooking(db, client.Id, staff.Id,
                DateTimeOffset.UtcNow.AddDays(i),
                DateTimeOffset.UtcNow.AddDays(i).AddMinutes(30));
        }

        var handler = new GetDashboardHandler(db, tenantContext);
        var result = await handler.Handle(new GetDashboardQuery());

        Assert.Equal(5, result.UpcomingBookings.Count);
        for (var i = 1; i < result.UpcomingBookings.Count; i++)
        {
            Assert.True(result.UpcomingBookings[i].StartTime >= result.UpcomingBookings[i - 1].StartTime);
        }
    }

    [Fact]
    public async Task Handle_UpcomingBookings_ExcludesCancelledNoShowCompleted()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        var staff = CreateTestStaffMember(db, tenantContext.UserId.ToString());
        var client = CreateTestClient(db);

        CreateTestBooking(db, client.Id, staff.Id,
            DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(1).AddMinutes(30),
            cancelled: true);
        CreateTestBooking(db, client.Id, staff.Id,
            DateTimeOffset.UtcNow.AddDays(2), DateTimeOffset.UtcNow.AddDays(2).AddMinutes(30),
            noShow: true);
        CreateTestBooking(db, client.Id, staff.Id,
            DateTimeOffset.UtcNow.AddDays(3), DateTimeOffset.UtcNow.AddDays(3).AddMinutes(30),
            completed: true);
        CreateTestBooking(db, client.Id, staff.Id,
            DateTimeOffset.UtcNow.AddDays(4), DateTimeOffset.UtcNow.AddDays(4).AddMinutes(30));

        var handler = new GetDashboardHandler(db, tenantContext);
        var result = await handler.Handle(new GetDashboardQuery());

        Assert.Single(result.UpcomingBookings);
    }

    [Fact]
    public async Task Handle_StaffMemberRole_SeesOnlyOwnUpcomingBookings()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        tenantContext.UserRole = "staff_member";

        var ownStaff = CreateTestStaffMember(db, tenantContext.UserId.ToString());
        var otherStaff = CreateTestStaffMember(db, Guid.NewGuid().ToString(), "Other", "Staff");
        var client = CreateTestClient(db);

        CreateTestBooking(db, client.Id, ownStaff.Id,
            DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(1).AddMinutes(30));
        CreateTestBooking(db, client.Id, otherStaff.Id,
            DateTimeOffset.UtcNow.AddDays(2), DateTimeOffset.UtcNow.AddDays(2).AddMinutes(30));

        var handler = new GetDashboardHandler(db, tenantContext);
        var result = await handler.Handle(new GetDashboardQuery());

        Assert.Single(result.UpcomingBookings);
        Assert.Equal(ownStaff.Id, result.UpcomingBookings[0].StaffMemberId);
    }

    [Fact]
    public async Task Handle_UpcomingBookings_ReturnsFewerThan5WhenNotEnough()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        var staff = CreateTestStaffMember(db, tenantContext.UserId.ToString());
        var client = CreateTestClient(db);

        CreateTestBooking(db, client.Id, staff.Id,
            DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(1).AddMinutes(30));
        CreateTestBooking(db, client.Id, staff.Id,
            DateTimeOffset.UtcNow.AddDays(2), DateTimeOffset.UtcNow.AddDays(2).AddMinutes(30));

        var handler = new GetDashboardHandler(db, tenantContext);
        var result = await handler.Handle(new GetDashboardQuery());

        Assert.Equal(2, result.UpcomingBookings.Count);
    }

    // ==================== New clients this week ====================

    [Fact]
    public async Task Handle_OwnerRole_NewClientsThisWeek_ReturnsCorrectCount()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        tenantContext.UserRole = "owner";
        CreateTestStaffMember(db, tenantContext.UserId.ToString());

        // Client created this week
        CreateTestClient(db, createdAtUtc: WeekStart().AddHours(1));
        CreateTestClient(db, createdAtUtc: WeekStart().AddDays(1));

        // Client created last week
        CreateTestClient(db, createdAtUtc: WeekStart().AddDays(-1));

        var handler = new GetDashboardHandler(db, tenantContext);
        var result = await handler.Handle(new GetDashboardQuery());

        Assert.Equal(2, result.NewClientsThisWeek);
    }

    [Fact]
    public async Task Handle_ManagerRole_NewClientsThisWeek_ReturnsCorrectCount()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        tenantContext.UserRole = "manager";
        CreateTestStaffMember(db, tenantContext.UserId.ToString());

        CreateTestClient(db, createdAtUtc: WeekStart().AddHours(1));

        var handler = new GetDashboardHandler(db, tenantContext);
        var result = await handler.Handle(new GetDashboardQuery());

        Assert.Equal(1, result.NewClientsThisWeek);
    }

    [Fact]
    public async Task Handle_StaffMemberRole_NewClientsThisWeek_ReturnsZero()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        tenantContext.UserRole = "staff_member";
        CreateTestStaffMember(db, tenantContext.UserId.ToString());

        CreateTestClient(db, createdAtUtc: WeekStart().AddHours(1));

        var handler = new GetDashboardHandler(db, tenantContext);
        var result = await handler.Handle(new GetDashboardQuery());

        Assert.Equal(0, result.NewClientsThisWeek);
    }

    [Fact]
    public async Task Handle_NewClientsThisWeek_CountsOnlyClientsSinceMonday()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        tenantContext.UserRole = "owner";
        CreateTestStaffMember(db, tenantContext.UserId.ToString());

        // Client created exactly at week start
        CreateTestClient(db, createdAtUtc: WeekStart());
        // Client created before week start
        CreateTestClient(db, createdAtUtc: WeekStart().AddSeconds(-1));

        var handler = new GetDashboardHandler(db, tenantContext);
        var result = await handler.Handle(new GetDashboardQuery());

        Assert.Equal(1, result.NewClientsThisWeek);
    }

    // ==================== Revenue ====================

    [Fact]
    public async Task Handle_OwnerRole_RevenueThisWeek_ReturnsSumOfPaidInvoices()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        tenantContext.UserRole = "owner";
        CreateTestStaffMember(db, tenantContext.UserId.ToString());

        CreateTestInvoice(db, 100m, paidAtUtc: WeekStart().AddHours(1));
        CreateTestInvoice(db, 50m, paidAtUtc: WeekStart().AddDays(1));

        var handler = new GetDashboardHandler(db, tenantContext);
        var result = await handler.Handle(new GetDashboardQuery());

        Assert.Equal(150m, result.RevenueThisWeek);
    }

    [Fact]
    public async Task Handle_OwnerRole_RevenueThisWeek_ReturnsZeroWhenNoPaidInvoices()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        tenantContext.UserRole = "owner";
        CreateTestStaffMember(db, tenantContext.UserId.ToString());

        var handler = new GetDashboardHandler(db, tenantContext);
        var result = await handler.Handle(new GetDashboardQuery());

        Assert.NotNull(result.RevenueThisWeek);
        Assert.Equal(0m, result.RevenueThisWeek);
    }

    [Fact]
    public async Task Handle_OwnerRole_RevenueThisMonth_ReturnsZeroWhenNoPaidInvoices()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        tenantContext.UserRole = "owner";
        CreateTestStaffMember(db, tenantContext.UserId.ToString());

        var handler = new GetDashboardHandler(db, tenantContext);
        var result = await handler.Handle(new GetDashboardQuery());

        Assert.NotNull(result.RevenueThisMonth);
        Assert.Equal(0m, result.RevenueThisMonth);
    }

    [Fact]
    public async Task Handle_OwnerRole_RevenueExcludesVoidedInvoices()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        tenantContext.UserRole = "owner";
        CreateTestStaffMember(db, tenantContext.UserId.ToString());

        CreateTestInvoice(db, 100m, paidAtUtc: WeekStart().AddHours(1));
        CreateTestInvoice(db, 200m, paidAtUtc: WeekStart().AddHours(2), voidedAtUtc: DateTimeOffset.UtcNow);

        var handler = new GetDashboardHandler(db, tenantContext);
        var result = await handler.Handle(new GetDashboardQuery());

        Assert.Equal(100m, result.RevenueThisWeek);
    }

    [Fact]
    public async Task Handle_ManagerRole_RevenueThisWeek_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        tenantContext.UserRole = "manager";
        CreateTestStaffMember(db, tenantContext.UserId.ToString());

        var handler = new GetDashboardHandler(db, tenantContext);
        var result = await handler.Handle(new GetDashboardQuery());

        Assert.Null(result.RevenueThisWeek);
    }

    [Fact]
    public async Task Handle_StaffMemberRole_RevenueThisWeek_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        tenantContext.UserRole = "staff_member";
        CreateTestStaffMember(db, tenantContext.UserId.ToString());

        var handler = new GetDashboardHandler(db, tenantContext);
        var result = await handler.Handle(new GetDashboardQuery());

        Assert.Null(result.RevenueThisWeek);
    }

    [Fact]
    public async Task Handle_OwnerRole_RevenueThisMonth_ReturnsSumOfPaidInvoices()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        tenantContext.UserRole = "owner";
        CreateTestStaffMember(db, tenantContext.UserId.ToString());

        var monthStart = new DateTimeOffset(
            new DateTime(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            TimeSpan.Zero);

        CreateTestInvoice(db, 200m, paidAtUtc: monthStart.AddHours(1));
        CreateTestInvoice(db, 300m, paidAtUtc: monthStart.AddDays(5));

        var handler = new GetDashboardHandler(db, tenantContext);
        var result = await handler.Handle(new GetDashboardQuery());

        Assert.Equal(500m, result.RevenueThisMonth);
    }

    [Fact]
    public async Task Handle_NonOwnerRole_RevenueThisMonth_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var tenantContext = TestTenantContext.Create();
        tenantContext.UserRole = "manager";
        CreateTestStaffMember(db, tenantContext.UserId.ToString());

        var handler = new GetDashboardHandler(db, tenantContext);
        var result = await handler.Handle(new GetDashboardQuery());

        Assert.Null(result.RevenueThisMonth);
    }
}
