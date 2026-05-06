using Chairly.Api.Features.Clients.GetClientTimeline;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;

namespace Chairly.Tests.Features.Clients;

public class GetClientTimelineHandlerTests
{
    private static ChairlyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ChairlyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ChairlyDbContext(options);
    }

    private static Client CreateTestClient(ChairlyDbContext db, Guid? id = null, Guid? tenantId = null, bool deleted = false)
    {
        var client = new Client
        {
            Id = id ?? Guid.NewGuid(),
            TenantId = tenantId ?? TestTenantContext.DefaultTenantId,
            FirstName = "Anna",
            LastName = "Bakker",
            Email = "anna@example.com",
            PhoneNumber = "0612345678",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            DeletedAtUtc = deleted ? DateTimeOffset.UtcNow.AddDays(-1) : null,
            DeletedBy = deleted ? Guid.Empty : null,
        };
        db.Clients.Add(client);
        db.SaveChanges();
        return client;
    }

    private static StaffMember CreateTestStaffMember(ChairlyDbContext db, string firstName = "Pieter", string lastName = "de Vries", Guid? id = null)
    {
        var staffMember = new StaffMember
        {
            Id = id ?? Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
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
        bool cancelled = false,
        bool noShow = false,
        bool confirmed = false,
        Guid? id = null)
    {
        var booking = new Booking
        {
            Id = id ?? Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            ClientId = clientId,
            StaffMemberId = staffMemberId,
            StartTime = startTime,
            EndTime = startTime.AddHours(1),
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-7),
            CreatedBy = Guid.Empty,
            CompletedAtUtc = completed ? startTime.AddMinutes(55) : null,
            CompletedBy = completed ? Guid.Empty : null,
            CancelledAtUtc = cancelled ? startTime.AddMinutes(-30) : null,
            CancelledBy = cancelled ? Guid.Empty : null,
            NoShowAtUtc = noShow ? startTime.AddMinutes(15) : null,
            NoShowBy = noShow ? Guid.Empty : null,
            ConfirmedAtUtc = confirmed ? startTime.AddDays(-1) : null,
            ConfirmedBy = confirmed ? Guid.Empty : null,
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

    private static Recipe CreateTestRecipe(ChairlyDbContext db, Guid bookingId, Guid clientId, Guid staffMemberId)
    {
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            BookingId = bookingId,
            ClientId = clientId,
            StaffMemberId = staffMemberId,
            Title = "Volledige kleuring",
            Notes = "Klant wil warme tonen",
            Products =
            [
                new RecipeProduct
                {
                    Id = Guid.NewGuid(),
                    Name = "Wella Illumina",
                    Brand = "Wella",
                    Quantity = "60 ml",
                    SortOrder = 0,
                },
            ],
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
        };
        db.Recipes.Add(recipe);
        db.SaveChanges();
        return recipe;
    }

    private static Invoice CreateTestInvoice(ChairlyDbContext db, Guid bookingId, Guid clientId, decimal totalAmount = 35.00m, bool voided = false, bool paid = false)
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            BookingId = bookingId,
            ClientId = clientId,
            InvoiceNumber = "INV-001",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            SubTotalAmount = totalAmount,
            TotalVatAmount = 0,
            TotalAmount = totalAmount,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            VoidedAtUtc = voided ? DateTimeOffset.UtcNow : null,
            VoidedBy = voided ? Guid.Empty : null,
            PaidAtUtc = paid ? DateTimeOffset.UtcNow : null,
            PaidBy = paid ? Guid.Empty : null,
        };
        db.Invoices.Add(invoice);
        db.SaveChanges();
        return invoice;
    }

    [Fact]
    public async Task Handle_ClientDoesNotExist_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new GetClientTimelineHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new GetClientTimelineQuery(Guid.NewGuid()));

        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task Handle_ClientBelongsToDifferentTenant_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var otherTenantId = Guid.NewGuid();
        CreateTestClient(db, tenantId: otherTenantId);
        var handler = new GetClientTimelineHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new GetClientTimelineQuery(db.Clients.First().Id));

        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task Handle_ClientIsSoftDeleted_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db, deleted: true);
        var handler = new GetClientTimelineHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new GetClientTimelineQuery(client.Id));

        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task Handle_ClientHasNoBookings_ReturnsEmptyTimelineAndZeroedStats()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var handler = new GetClientTimelineHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new GetClientTimelineQuery(client.Id));

        var response = result.AsT0;
        Assert.Equal(client.Id, response.Profile.Id);
        Assert.Empty(response.Timeline);
        Assert.Equal(0, response.Stats.TotalVisits);
        Assert.Null(response.Stats.LastVisitAtUtc);
        Assert.Equal(0m, response.Stats.TotalSpentAmount);
        Assert.Null(response.Stats.MostVisitedStaffMember);
        Assert.Null(response.Stats.MostBookedService);
        Assert.Equal(0, response.Stats.NoShowCount);
    }

    [Fact]
    public async Task Handle_MixedStatusBookings_ComputesStatsCorrectly()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);

        // 2 completed, 1 cancelled, 1 no-show, 1 scheduled
        var b1 = CreateTestBooking(db, client.Id, staff.Id, DateTimeOffset.UtcNow.AddDays(-10), completed: true);
        var b2 = CreateTestBooking(db, client.Id, staff.Id, DateTimeOffset.UtcNow.AddDays(-5), completed: true);
        CreateTestBooking(db, client.Id, staff.Id, DateTimeOffset.UtcNow.AddDays(-3), cancelled: true);
        CreateTestBooking(db, client.Id, staff.Id, DateTimeOffset.UtcNow.AddDays(-1), noShow: true);
        CreateTestBooking(db, client.Id, staff.Id, DateTimeOffset.UtcNow.AddDays(1));

        CreateTestInvoice(db, b1.Id, client.Id, 35.00m);
        CreateTestInvoice(db, b2.Id, client.Id, 50.00m);

        var handler = new GetClientTimelineHandler(db, TestTenantContext.Create());
        var result = await handler.Handle(new GetClientTimelineQuery(client.Id));

        var stats = result.AsT0.Stats;
        Assert.Equal(2, stats.TotalVisits);
        Assert.NotNull(stats.LastVisitAtUtc);
        Assert.Equal(85.00m, stats.TotalSpentAmount);
        Assert.Equal(1, stats.NoShowCount);
        Assert.Equal(5, result.AsT0.Timeline.Count);
    }

    [Fact]
    public async Task Handle_TotalSpentAmount_ExcludesVoidedInvoices()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);

        var b1 = CreateTestBooking(db, client.Id, staff.Id, DateTimeOffset.UtcNow.AddDays(-5), completed: true);
        var b2 = CreateTestBooking(db, client.Id, staff.Id, DateTimeOffset.UtcNow.AddDays(-3), completed: true);

        CreateTestInvoice(db, b1.Id, client.Id, 100.00m);
        CreateTestInvoice(db, b2.Id, client.Id, 50.00m, voided: true);

        var handler = new GetClientTimelineHandler(db, TestTenantContext.Create());
        var result = await handler.Handle(new GetClientTimelineQuery(client.Id));

        Assert.Equal(100.00m, result.AsT0.Stats.TotalSpentAmount);
    }

    [Fact]
    public async Task Handle_MostVisitedStaffMember_CorrectlyIdentified()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staffA = CreateTestStaffMember(db, "Jan", "de Groot");
        var staffB = CreateTestStaffMember(db, "Lisa", "Smit");

        // staffA has 3 completed bookings, staffB has 1
        CreateTestBooking(db, client.Id, staffA.Id, DateTimeOffset.UtcNow.AddDays(-10), completed: true);
        CreateTestBooking(db, client.Id, staffA.Id, DateTimeOffset.UtcNow.AddDays(-8), completed: true);
        CreateTestBooking(db, client.Id, staffA.Id, DateTimeOffset.UtcNow.AddDays(-6), completed: true);
        CreateTestBooking(db, client.Id, staffB.Id, DateTimeOffset.UtcNow.AddDays(-4), completed: true);

        var handler = new GetClientTimelineHandler(db, TestTenantContext.Create());
        var result = await handler.Handle(new GetClientTimelineQuery(client.Id));

        var mostVisited = result.AsT0.Stats.MostVisitedStaffMember;
        Assert.NotNull(mostVisited);
        Assert.Equal(staffA.Id, mostVisited.Id);
        Assert.Equal("Jan de Groot", mostVisited.FullName);
        Assert.Equal(3, mostVisited.VisitCount);
    }

    [Fact]
    public async Task Handle_OnlyScheduledAndCancelledBookings_MostVisitedStaffMemberIsNull()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);

        CreateTestBooking(db, client.Id, staff.Id, DateTimeOffset.UtcNow.AddDays(1));
        CreateTestBooking(db, client.Id, staff.Id, DateTimeOffset.UtcNow.AddDays(-1), cancelled: true);

        var handler = new GetClientTimelineHandler(db, TestTenantContext.Create());
        var result = await handler.Handle(new GetClientTimelineQuery(client.Id));

        Assert.Null(result.AsT0.Stats.MostVisitedStaffMember);
    }

    [Fact]
    public async Task Handle_MostBookedService_CorrectlyIdentified()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);

        var serviceIdA = Guid.NewGuid();
        var serviceIdB = Guid.NewGuid();

        // Create bookings with specific services
        var b1 = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddDays(-5),
            EndTime = DateTimeOffset.UtcNow.AddDays(-5).AddHours(1),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            CompletedAtUtc = DateTimeOffset.UtcNow.AddDays(-5),
            CompletedBy = Guid.Empty,
            BookingServices =
            [
                new BookingService { Id = Guid.NewGuid(), ServiceId = serviceIdA, ServiceName = "Knippen", Duration = TimeSpan.FromMinutes(30), Price = 35.00m, SortOrder = 0 },
                new BookingService { Id = Guid.NewGuid(), ServiceId = serviceIdB, ServiceName = "Wassen", Duration = TimeSpan.FromMinutes(10), Price = 10.00m, SortOrder = 1 },
            ],
        };
        db.Bookings.Add(b1);

        var b2 = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddDays(-3),
            EndTime = DateTimeOffset.UtcNow.AddDays(-3).AddHours(1),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            CompletedAtUtc = DateTimeOffset.UtcNow.AddDays(-3),
            CompletedBy = Guid.Empty,
            BookingServices =
            [
                new BookingService { Id = Guid.NewGuid(), ServiceId = serviceIdA, ServiceName = "Knippen", Duration = TimeSpan.FromMinutes(30), Price = 35.00m, SortOrder = 0 },
            ],
        };
        db.Bookings.Add(b2);
        await db.SaveChangesAsync();

        var handler = new GetClientTimelineHandler(db, TestTenantContext.Create());
        var result = await handler.Handle(new GetClientTimelineQuery(client.Id));

        var mostBooked = result.AsT0.Stats.MostBookedService;
        Assert.NotNull(mostBooked);
        Assert.Equal(serviceIdA, mostBooked.Id);
        Assert.Equal("Knippen", mostBooked.Name);
        Assert.Equal(2, mostBooked.BookingCount);
    }

    [Fact]
    public async Task Handle_RecipeInlinedOnMatchingBooking_NullOnOthers()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);

        var bookingWithRecipe = CreateTestBooking(db, client.Id, staff.Id, DateTimeOffset.UtcNow.AddDays(-5), completed: true);
        var bookingWithoutRecipe = CreateTestBooking(db, client.Id, staff.Id, DateTimeOffset.UtcNow.AddDays(-3), completed: true);

        CreateTestRecipe(db, bookingWithRecipe.Id, client.Id, staff.Id);

        var handler = new GetClientTimelineHandler(db, TestTenantContext.Create());
        var result = await handler.Handle(new GetClientTimelineQuery(client.Id));

        var timeline = result.AsT0.Timeline;
        Assert.Equal(2, timeline.Count);

        // Timeline is ordered by StartTime DESC, so bookingWithoutRecipe (more recent) comes first
        var entryWithoutRecipe = timeline.First(e => e.Booking.Id == bookingWithoutRecipe.Id);
        var entryWithRecipe = timeline.First(e => e.Booking.Id == bookingWithRecipe.Id);

        Assert.Null(entryWithoutRecipe.Recipe);
        Assert.NotNull(entryWithRecipe.Recipe);
        Assert.Equal("Volledige kleuring", entryWithRecipe.Recipe.Title);
    }

    [Fact]
    public async Task Handle_InvoiceInlinedOnMatchingBooking_NullOnOthers()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);

        var bookingWithInvoice = CreateTestBooking(db, client.Id, staff.Id, DateTimeOffset.UtcNow.AddDays(-5), completed: true);
        var bookingWithoutInvoice = CreateTestBooking(db, client.Id, staff.Id, DateTimeOffset.UtcNow.AddDays(-3), completed: true);

        CreateTestInvoice(db, bookingWithInvoice.Id, client.Id, 50.00m);

        var handler = new GetClientTimelineHandler(db, TestTenantContext.Create());
        var result = await handler.Handle(new GetClientTimelineQuery(client.Id));

        var timeline = result.AsT0.Timeline;
        var entryWithInvoice = timeline.First(e => e.Booking.Id == bookingWithInvoice.Id);
        var entryWithoutInvoice = timeline.First(e => e.Booking.Id == bookingWithoutInvoice.Id);

        Assert.NotNull(entryWithInvoice.Invoice);
        Assert.Equal("INV-001", entryWithInvoice.Invoice.InvoiceNumber);
        Assert.Null(entryWithoutInvoice.Invoice);
    }

    [Fact]
    public async Task Handle_TimelineOrderedByStartTimeDescending()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);

        var oldest = CreateTestBooking(db, client.Id, staff.Id, DateTimeOffset.UtcNow.AddDays(-10));
        var middle = CreateTestBooking(db, client.Id, staff.Id, DateTimeOffset.UtcNow.AddDays(-5));
        var newest = CreateTestBooking(db, client.Id, staff.Id, DateTimeOffset.UtcNow.AddDays(-1));

        var handler = new GetClientTimelineHandler(db, TestTenantContext.Create());
        var result = await handler.Handle(new GetClientTimelineQuery(client.Id));

        var timeline = result.AsT0.Timeline;
        Assert.Equal(3, timeline.Count);
        Assert.Equal(newest.Id, timeline[0].Booking.Id);
        Assert.Equal(middle.Id, timeline[1].Booking.Id);
        Assert.Equal(oldest.Id, timeline[2].Booking.Id);
    }

    [Fact]
    public async Task Handle_BookingStatusesDerivedCorrectly()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);

        CreateTestBooking(db, client.Id, staff.Id, DateTimeOffset.UtcNow.AddDays(1)); // Scheduled
        CreateTestBooking(db, client.Id, staff.Id, DateTimeOffset.UtcNow.AddDays(-1), confirmed: true); // Confirmed
        CreateTestBooking(db, client.Id, staff.Id, DateTimeOffset.UtcNow.AddDays(-3), completed: true); // Completed
        CreateTestBooking(db, client.Id, staff.Id, DateTimeOffset.UtcNow.AddDays(-5), cancelled: true); // Cancelled
        CreateTestBooking(db, client.Id, staff.Id, DateTimeOffset.UtcNow.AddDays(-7), noShow: true); // NoShow

        var handler = new GetClientTimelineHandler(db, TestTenantContext.Create());
        var result = await handler.Handle(new GetClientTimelineQuery(client.Id));

        var statuses = result.AsT0.Timeline.Select(e => e.Booking.Status).ToList();
        Assert.Contains("Scheduled", statuses);
        Assert.Contains("Confirmed", statuses);
        Assert.Contains("Completed", statuses);
        Assert.Contains("Cancelled", statuses);
        Assert.Contains("NoShow", statuses);
    }

    [Fact]
    public async Task Handle_ProfileFieldsMappedCorrectly()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var handler = new GetClientTimelineHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new GetClientTimelineQuery(client.Id));

        var profile = result.AsT0.Profile;
        Assert.Equal("Anna", profile.FirstName);
        Assert.Equal("Bakker", profile.LastName);
        Assert.Equal("anna@example.com", profile.Email);
        Assert.Equal("0612345678", profile.PhoneNumber);
    }

    [Fact]
    public async Task Handle_BookingCardFieldsMappedCorrectly()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var startTime = DateTimeOffset.UtcNow.AddDays(-3);

        var booking = CreateTestBooking(db, client.Id, staff.Id, startTime, completed: true);

        var handler = new GetClientTimelineHandler(db, TestTenantContext.Create());
        var result = await handler.Handle(new GetClientTimelineQuery(client.Id));

        var card = result.AsT0.Timeline[0].Booking;
        Assert.Equal(booking.Id, card.Id);
        Assert.Equal(60, card.DurationMinutes);
        Assert.Equal("Pieter de Vries", card.StaffMemberName);
        Assert.Equal(35.00m, card.TotalPrice);
        Assert.Single(card.Services);
        Assert.Equal("Knippen", card.Services[0].ServiceName);
        Assert.Equal(30, card.Services[0].DurationMinutes);
    }

    [Fact]
    public async Task Handle_DraftInvoiceIncludedInTotalSpent()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);

        var b1 = CreateTestBooking(db, client.Id, staff.Id, DateTimeOffset.UtcNow.AddDays(-5), completed: true);

        // Draft invoice (no SentAtUtc, no PaidAtUtc)
        CreateTestInvoice(db, b1.Id, client.Id, 75.00m);

        var handler = new GetClientTimelineHandler(db, TestTenantContext.Create());
        var result = await handler.Handle(new GetClientTimelineQuery(client.Id));

        Assert.Equal(75.00m, result.AsT0.Stats.TotalSpentAmount);
    }
}
