using Chairly.Api.Features.Bookings;
using Chairly.Api.Features.Bookings.CancelBooking;
using Chairly.Api.Features.Bookings.CompleteBooking;
using Chairly.Api.Features.Bookings.ConfirmBooking;
using Chairly.Api.Features.Bookings.CreateBooking;
using Chairly.Api.Features.Bookings.GetBooking;
using Chairly.Api.Features.Bookings.GetBookingsList;
using Chairly.Api.Features.Bookings.MarkBookingNoShow;
using Chairly.Api.Features.Bookings.StartBooking;
using Chairly.Api.Features.Bookings.UpdateBooking;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Chairly.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OneOf.Types;

namespace Chairly.Tests.Features.Bookings;

public class BookingHandlerTests
{
    private static ChairlyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ChairlyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ChairlyDbContext(options);
    }

    private static Client CreateTestClient(ChairlyDbContext db)
    {
        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            FirstName = "Test",
            LastName = "Client",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Clients.Add(client);
        db.SaveChanges();
        return client;
    }

    private static StaffMember CreateTestStaffMember(ChairlyDbContext db)
    {
        var staffMember = new StaffMember
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            FirstName = "Test",
            LastName = "Staff",
            Color = "#000000",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.StaffMembers.Add(staffMember);
        db.SaveChanges();
        return staffMember;
    }

    private static Service CreateTestService(ChairlyDbContext db, TimeSpan? duration = null, decimal price = 25.00m, string name = "Test Service")
    {
        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            Name = name,
            Duration = duration ?? TimeSpan.FromMinutes(30),
            Price = price,
            IsActive = true,
            SortOrder = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Services.Add(service);
        db.SaveChanges();
        return service;
    }

    private static Booking CreateTestBooking(
        ChairlyDbContext db,
        Guid clientId,
        Guid staffMemberId,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null,
        bool includeService = true)
    {
        var start = startTime ?? DateTimeOffset.UtcNow.AddHours(1);
        var end = endTime ?? start.AddMinutes(30);
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            ClientId = clientId,
            StaffMemberId = staffMemberId,
            StartTime = start,
            EndTime = end,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        if (includeService)
        {
            booking.BookingServices.Add(new BookingService
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                ServiceId = Guid.NewGuid(),
                ServiceName = "Test Service",
                Duration = TimeSpan.FromMinutes(30),
                Price = 25.00m,
                SortOrder = 0,
            });
        }

        db.Bookings.Add(booking);
        db.SaveChanges();
        return booking;
    }

    // ==================== B3: Status derivation ====================

    [Fact]
    public void DeriveStatus_NoTimestamps_ReturnsScheduled()
    {
        var booking = new Booking();
        Assert.Equal("Scheduled", BookingMapper.DeriveStatus(booking));
    }

    [Fact]
    public void DeriveStatus_ConfirmedAtUtcSet_ReturnsConfirmed()
    {
        var booking = new Booking { ConfirmedAtUtc = DateTimeOffset.UtcNow };
        Assert.Equal("Confirmed", BookingMapper.DeriveStatus(booking));
    }

    [Fact]
    public void DeriveStatus_StartedAtUtcSet_ReturnsInProgress()
    {
        var booking = new Booking { StartedAtUtc = DateTimeOffset.UtcNow };
        Assert.Equal("InProgress", BookingMapper.DeriveStatus(booking));
    }

    [Fact]
    public void DeriveStatus_CompletedAtUtcSet_ReturnsCompleted()
    {
        var booking = new Booking { StartedAtUtc = DateTimeOffset.UtcNow, CompletedAtUtc = DateTimeOffset.UtcNow };
        Assert.Equal("Completed", BookingMapper.DeriveStatus(booking));
    }

    [Fact]
    public void DeriveStatus_CancelledAtUtcSet_ReturnsCancelled()
    {
        var booking = new Booking { CancelledAtUtc = DateTimeOffset.UtcNow };
        Assert.Equal("Cancelled", BookingMapper.DeriveStatus(booking));
    }

    [Fact]
    public void DeriveStatus_NoShowAtUtcSet_ReturnsNoShow()
    {
        var booking = new Booking { NoShowAtUtc = DateTimeOffset.UtcNow };
        Assert.Equal("NoShow", BookingMapper.DeriveStatus(booking));
    }

    [Fact]
    public void DeriveStatus_CancelledAndCompleted_ReturnsCancelled()
    {
        var booking = new Booking
        {
            CompletedAtUtc = DateTimeOffset.UtcNow,
            CancelledAtUtc = DateTimeOffset.UtcNow,
        };
        Assert.Equal("Cancelled", BookingMapper.DeriveStatus(booking));
    }

    // ==================== B4: Create Booking ====================

    [Fact]
    public async Task CreateBookingHandler_HappyPath_CreatesBookingWithCorrectFields()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db);
        var handler = new CreateBookingHandler(db, new NullBookingEventPublisher(), NullLogger<CreateBookingHandler>.Instance);
        var startTime = DateTimeOffset.UtcNow.AddHours(1);

        var command = new CreateBookingCommand
        {
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = startTime,
            ServiceIds = [service.Id],
            Notes = "Test booking",
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(client.Id, response.ClientId);
        Assert.Equal(staff.Id, response.StaffMemberId);
        Assert.Equal(startTime, response.StartTime);
        Assert.Equal("Test booking", response.Notes);
        Assert.Equal("Scheduled", response.Status);
        Assert.Single(response.Services);
    }

    [Fact]
    public async Task CreateBookingHandler_SnapshotsServiceData()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db, TimeSpan.FromMinutes(45), 35.00m, "Haircut");
        var handler = new CreateBookingHandler(db, new NullBookingEventPublisher(), NullLogger<CreateBookingHandler>.Instance);

        var command = new CreateBookingCommand
        {
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [service.Id],
        };

        var result = await handler.Handle(command);

        var svc = result.AsT0.Services[0];
        Assert.Equal("Haircut", svc.ServiceName);
        Assert.Equal(TimeSpan.FromMinutes(45), svc.Duration);
        Assert.Equal(35.00m, svc.Price);
    }

    [Fact]
    public async Task CreateBookingHandler_CalculatesEndTimeCorrectly()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service1 = CreateTestService(db, TimeSpan.FromMinutes(30), name: "Service 1");
        var service2 = CreateTestService(db, TimeSpan.FromMinutes(15), name: "Service 2");
        var handler = new CreateBookingHandler(db, new NullBookingEventPublisher(), NullLogger<CreateBookingHandler>.Instance);
        var startTime = DateTimeOffset.UtcNow.AddHours(1);

        var command = new CreateBookingCommand
        {
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = startTime,
            ServiceIds = [service1.Id, service2.Id],
        };

        var result = await handler.Handle(command);

        Assert.Equal(startTime.AddMinutes(45), result.AsT0.EndTime);
    }

    [Fact]
    public async Task CreateBookingHandler_ClientNotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db);
        var handler = new CreateBookingHandler(db, new NullBookingEventPublisher(), NullLogger<CreateBookingHandler>.Instance);

        var command = new CreateBookingCommand
        {
            ClientId = Guid.NewGuid(),
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [service.Id],
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task CreateBookingHandler_ClientDeleted_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        client.DeletedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db);
        var handler = new CreateBookingHandler(db, new NullBookingEventPublisher(), NullLogger<CreateBookingHandler>.Instance);

        var command = new CreateBookingCommand
        {
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [service.Id],
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task CreateBookingHandler_StaffMemberNotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var service = CreateTestService(db);
        var handler = new CreateBookingHandler(db, new NullBookingEventPublisher(), NullLogger<CreateBookingHandler>.Instance);

        var command = new CreateBookingCommand
        {
            ClientId = client.Id,
            StaffMemberId = Guid.NewGuid(),
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [service.Id],
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task CreateBookingHandler_StaffMemberDeactivated_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        staff.DeactivatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var service = CreateTestService(db);
        var handler = new CreateBookingHandler(db, new NullBookingEventPublisher(), NullLogger<CreateBookingHandler>.Instance);

        var command = new CreateBookingCommand
        {
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [service.Id],
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task CreateBookingHandler_ServiceNotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var handler = new CreateBookingHandler(db, new NullBookingEventPublisher(), NullLogger<CreateBookingHandler>.Instance);

        var command = new CreateBookingCommand
        {
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [Guid.NewGuid()],
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task CreateBookingHandler_ServiceInactive_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db);
        service.IsActive = false;
        await db.SaveChangesAsync();
        var handler = new CreateBookingHandler(db, new NullBookingEventPublisher(), NullLogger<CreateBookingHandler>.Instance);

        var command = new CreateBookingCommand
        {
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [service.Id],
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task CreateBookingHandler_OverlappingBooking_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db);
        var startTime = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        CreateTestBooking(db, client.Id, staff.Id, startTime, startTime.AddMinutes(30));
        var handler = new CreateBookingHandler(db, new NullBookingEventPublisher(), NullLogger<CreateBookingHandler>.Instance);

        var command = new CreateBookingCommand
        {
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = startTime.AddMinutes(15),
            ServiceIds = [service.Id],
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT2);
        Assert.IsType<Conflict>(result.AsT2);
    }

    [Fact]
    public async Task CreateBookingHandler_CancelledBookingDoesNotConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db);
        var startTime = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var existingBooking = CreateTestBooking(db, client.Id, staff.Id, startTime, startTime.AddMinutes(30));
        existingBooking.CancelledAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new CreateBookingHandler(db, new NullBookingEventPublisher(), NullLogger<CreateBookingHandler>.Instance);

        var command = new CreateBookingCommand
        {
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = startTime.AddMinutes(15),
            ServiceIds = [service.Id],
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT0);
    }

    [Fact]
    public async Task CreateBookingHandler_NoShowBookingDoesNotConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db);
        var startTime = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var existingBooking = CreateTestBooking(db, client.Id, staff.Id, startTime, startTime.AddMinutes(30));
        existingBooking.NoShowAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new CreateBookingHandler(db, new NullBookingEventPublisher(), NullLogger<CreateBookingHandler>.Instance);

        var command = new CreateBookingCommand
        {
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = startTime.AddMinutes(15),
            ServiceIds = [service.Id],
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT0);
    }

    [Fact]
    public async Task CreateBookingHandler_MultipleServices_SavesCorrectSortOrder()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service1 = CreateTestService(db, name: "First");
        var service2 = CreateTestService(db, name: "Second");
        var handler = new CreateBookingHandler(db, new NullBookingEventPublisher(), NullLogger<CreateBookingHandler>.Instance);

        var command = new CreateBookingCommand
        {
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [service1.Id, service2.Id],
        };

        var result = await handler.Handle(command);

        var services = result.AsT0.Services;
        Assert.Equal(2, services.Count);
        Assert.Equal("First", services[0].ServiceName);
        Assert.Equal(0, services[0].SortOrder);
        Assert.Equal("Second", services[1].ServiceName);
        Assert.Equal(1, services[1].SortOrder);
    }

    // ==================== B5: Get Booking ====================

    [Fact]
    public async Task GetBookingHandler_HappyPath_ReturnsBookingWithServices()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        var handler = new GetBookingHandler(db);

        var result = await handler.Handle(new GetBookingQuery(booking.Id));

        var response = result.AsT0;
        Assert.Equal(booking.Id, response.Id);
        Assert.Single(response.Services);
        Assert.Equal("Scheduled", response.Status);
    }

    [Fact]
    public async Task GetBookingHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new GetBookingHandler(db);

        var result = await handler.Handle(new GetBookingQuery(Guid.NewGuid()));

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }

    // ==================== B6: Get Bookings List ====================

    [Fact]
    public async Task GetBookingsListHandler_HappyPath_ReturnsAllBookingsOrderedByStartTime()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var startTime1 = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var startTime2 = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        CreateTestBooking(db, client.Id, staff.Id, startTime1, startTime1.AddMinutes(30));
        CreateTestBooking(db, client.Id, staff.Id, startTime2, startTime2.AddMinutes(30));
        var handler = new GetBookingsListHandler(db);

        var result = (await handler.Handle(new GetBookingsListQuery(null, null))).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(startTime2, result[0].StartTime);
        Assert.Equal(startTime1, result[1].StartTime);
    }

    [Fact]
    public async Task GetBookingsListHandler_FilterByDate_ReturnsOnlyMatchingBookings()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var date1 = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var date2 = new DateTimeOffset(2026, 6, 2, 10, 0, 0, TimeSpan.Zero);
        CreateTestBooking(db, client.Id, staff.Id, date1, date1.AddMinutes(30));
        CreateTestBooking(db, client.Id, staff.Id, date2, date2.AddMinutes(30));
        var handler = new GetBookingsListHandler(db);

        var result = (await handler.Handle(new GetBookingsListQuery(new DateOnly(2026, 6, 1), null))).ToList();

        Assert.Single(result);
        Assert.Equal(date1, result[0].StartTime);
    }

    [Fact]
    public async Task GetBookingsListHandler_FilterByStaffMemberId_ReturnsOnlyMatchingBookings()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff1 = CreateTestStaffMember(db);
        var staff2 = CreateTestStaffMember(db);
        var startTime = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        CreateTestBooking(db, client.Id, staff1.Id, startTime, startTime.AddMinutes(30));
        CreateTestBooking(db, client.Id, staff2.Id, startTime.AddHours(1), startTime.AddHours(1).AddMinutes(30));
        var handler = new GetBookingsListHandler(db);

        var result = (await handler.Handle(new GetBookingsListQuery(null, staff1.Id))).ToList();

        Assert.Single(result);
        Assert.Equal(staff1.Id, result[0].StaffMemberId);
    }

    [Fact]
    public async Task GetBookingsListHandler_BothFilters_ReturnsCorrectBookings()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff1 = CreateTestStaffMember(db);
        var staff2 = CreateTestStaffMember(db);
        var date1Time = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var date2Time = new DateTimeOffset(2026, 6, 2, 10, 0, 0, TimeSpan.Zero);
        CreateTestBooking(db, client.Id, staff1.Id, date1Time, date1Time.AddMinutes(30));
        CreateTestBooking(db, client.Id, staff1.Id, date2Time, date2Time.AddMinutes(30));
        CreateTestBooking(db, client.Id, staff2.Id, date1Time.AddHours(2), date1Time.AddHours(2).AddMinutes(30));
        var handler = new GetBookingsListHandler(db);

        var result = (await handler.Handle(new GetBookingsListQuery(new DateOnly(2026, 6, 1), staff1.Id))).ToList();

        Assert.Single(result);
        Assert.Equal(staff1.Id, result[0].StaffMemberId);
    }

    [Fact]
    public async Task GetBookingsListHandler_NoMatch_ReturnsEmptyList()
    {
        await using var db = CreateDbContext();
        var handler = new GetBookingsListHandler(db);

        var result = (await handler.Handle(new GetBookingsListQuery(null, null))).ToList();

        Assert.Empty(result);
    }

    // ==================== B7: Update Booking ====================

    [Fact]
    public async Task UpdateBookingHandler_HappyPath_UpdatesBooking()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db);

        // Create booking via handler to ensure proper EF change tracking with InMemory provider
        var createHandler = new CreateBookingHandler(db, new NullBookingEventPublisher(), NullLogger<CreateBookingHandler>.Instance);
        var createResult = await createHandler.Handle(new CreateBookingCommand
        {
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [service.Id],
        });
        var bookingId = createResult.AsT0.Id;

        var handler = new UpdateBookingHandler(db);
        var newStartTime = DateTimeOffset.UtcNow.AddHours(5);

        var command = new UpdateBookingCommand
        {
            Id = bookingId,
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = newStartTime,
            ServiceIds = [service.Id],
            Notes = "Updated notes",
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Equal("Updated notes", response.Notes);
        Assert.Equal(newStartTime, response.StartTime);
        Assert.NotNull(response.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateBookingHandler_BookingNotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db);
        var handler = new UpdateBookingHandler(db);

        var command = new UpdateBookingCommand
        {
            Id = Guid.NewGuid(),
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [service.Id],
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task UpdateBookingHandler_CompletedBooking_AllowsUpdate()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.StartedAtUtc = DateTimeOffset.UtcNow;
        booking.CompletedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new UpdateBookingHandler(db);

        var command = new UpdateBookingCommand
        {
            Id = booking.Id,
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [service.Id],
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT0);
        var response = result.AsT0;
        Assert.Equal(booking.Id, response.Id);
        Assert.Single(response.Services);
    }

    [Fact]
    public async Task UpdateBookingHandler_CancelledBooking_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.CancelledAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new UpdateBookingHandler(db);

        var command = new UpdateBookingCommand
        {
            Id = booking.Id,
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [service.Id],
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task UpdateBookingHandler_NoShowBooking_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.NoShowAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new UpdateBookingHandler(db);

        var command = new UpdateBookingCommand
        {
            Id = booking.Id,
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [service.Id],
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task UpdateBookingHandler_ClientNotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        var handler = new UpdateBookingHandler(db);

        var command = new UpdateBookingCommand
        {
            Id = booking.Id,
            ClientId = Guid.NewGuid(),
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [service.Id],
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task UpdateBookingHandler_StaffDeactivated_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        staff.DeactivatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new UpdateBookingHandler(db);

        var command = new UpdateBookingCommand
        {
            Id = booking.Id,
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [service.Id],
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task UpdateBookingHandler_ServiceInactive_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        service.IsActive = false;
        await db.SaveChangesAsync();
        var handler = new UpdateBookingHandler(db);

        var command = new UpdateBookingCommand
        {
            Id = booking.Id,
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [service.Id],
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task UpdateBookingHandler_SelfOverlapAllowed()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db);
        var startTime = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

        // Create booking via handler for proper EF change tracking with InMemory provider
        var createHandler = new CreateBookingHandler(db, new NullBookingEventPublisher(), NullLogger<CreateBookingHandler>.Instance);
        var createResult = await createHandler.Handle(new CreateBookingCommand
        {
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = startTime,
            ServiceIds = [service.Id],
        });
        var bookingId = createResult.AsT0.Id;

        var handler = new UpdateBookingHandler(db);

        var command = new UpdateBookingCommand
        {
            Id = bookingId,
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = startTime.AddMinutes(10),
            ServiceIds = [service.Id],
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT0);
    }

    [Fact]
    public async Task UpdateBookingHandler_OverlapWithOtherBooking_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db);
        var startTime = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        CreateTestBooking(db, client.Id, staff.Id, startTime, startTime.AddMinutes(30));
        var booking2 = CreateTestBooking(db, client.Id, staff.Id, startTime.AddHours(2), startTime.AddHours(2).AddMinutes(30));
        var handler = new UpdateBookingHandler(db);

        var command = new UpdateBookingCommand
        {
            Id = booking2.Id,
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = startTime.AddMinutes(15),
            ServiceIds = [service.Id],
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task UpdateBookingHandler_ReplacesBookingServices()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service1 = CreateTestService(db, name: "Old Service");
        var service2 = CreateTestService(db, name: "New Service");

        // Create booking via handler for proper EF change tracking with InMemory provider
        var createHandler = new CreateBookingHandler(db, new NullBookingEventPublisher(), NullLogger<CreateBookingHandler>.Instance);
        var createResult = await createHandler.Handle(new CreateBookingCommand
        {
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [service1.Id],
        });
        var bookingId = createResult.AsT0.Id;

        var handler = new UpdateBookingHandler(db);

        var command = new UpdateBookingCommand
        {
            Id = bookingId,
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddHours(5),
            ServiceIds = [service2.Id],
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Single(response.Services);
        Assert.Equal("New Service", response.Services[0].ServiceName);
    }

    // ==================== B8: Cancel Booking ====================

    [Fact]
    public async Task CancelBookingHandler_FromScheduled_SetsCancelledAtUtc()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        var handler = new CancelBookingHandler(db, new NullBookingEventPublisher(), NullLogger<CancelBookingHandler>.Instance);

        var result = await handler.Handle(new CancelBookingCommand(booking.Id));

        Assert.True(result.IsT0);
        Assert.IsType<Success>(result.AsT0);
        var updated = await db.Bookings.FindAsync(booking.Id);
        Assert.NotNull(updated!.CancelledAtUtc);
    }

    [Fact]
    public async Task CancelBookingHandler_FromConfirmed_SetsCancelledAtUtc()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.ConfirmedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new CancelBookingHandler(db, new NullBookingEventPublisher(), NullLogger<CancelBookingHandler>.Instance);

        var result = await handler.Handle(new CancelBookingCommand(booking.Id));

        Assert.True(result.IsT0);
    }

    [Fact]
    public async Task CancelBookingHandler_FromInProgress_AllowedAndSetsCancelledAtUtc()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.StartedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new CancelBookingHandler(db, new NullBookingEventPublisher(), NullLogger<CancelBookingHandler>.Instance);

        var result = await handler.Handle(new CancelBookingCommand(booking.Id));

        Assert.True(result.IsT0);
    }

    [Fact]
    public async Task CancelBookingHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new CancelBookingHandler(db, new NullBookingEventPublisher(), NullLogger<CancelBookingHandler>.Instance);

        var result = await handler.Handle(new CancelBookingCommand(Guid.NewGuid()));

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task CancelBookingHandler_AlreadyCompleted_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.StartedAtUtc = DateTimeOffset.UtcNow;
        booking.CompletedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new CancelBookingHandler(db, new NullBookingEventPublisher(), NullLogger<CancelBookingHandler>.Instance);

        var result = await handler.Handle(new CancelBookingCommand(booking.Id));

        Assert.True(result.IsT2);
        Assert.IsType<Conflict>(result.AsT2);
    }

    [Fact]
    public async Task CancelBookingHandler_AlreadyCancelled_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.CancelledAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new CancelBookingHandler(db, new NullBookingEventPublisher(), NullLogger<CancelBookingHandler>.Instance);

        var result = await handler.Handle(new CancelBookingCommand(booking.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task CancelBookingHandler_AlreadyNoShow_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.NoShowAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new CancelBookingHandler(db, new NullBookingEventPublisher(), NullLogger<CancelBookingHandler>.Instance);

        var result = await handler.Handle(new CancelBookingCommand(booking.Id));

        Assert.True(result.IsT2);
    }

    // ==================== B9: Confirm Booking ====================

    [Fact]
    public async Task ConfirmBookingHandler_FromScheduled_SetsConfirmedAtUtc()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        var handler = new ConfirmBookingHandler(db, new NullBookingEventPublisher(), NullLogger<ConfirmBookingHandler>.Instance);

        var result = await handler.Handle(new ConfirmBookingCommand(booking.Id));

        Assert.True(result.IsT0);
        Assert.IsType<Success>(result.AsT0);
        var updated = await db.Bookings.FindAsync(booking.Id);
        Assert.NotNull(updated!.ConfirmedAtUtc);
    }

    [Fact]
    public async Task ConfirmBookingHandler_AlreadyConfirmed_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.ConfirmedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new ConfirmBookingHandler(db, new NullBookingEventPublisher(), NullLogger<ConfirmBookingHandler>.Instance);

        var result = await handler.Handle(new ConfirmBookingCommand(booking.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task ConfirmBookingHandler_InProgress_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.StartedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new ConfirmBookingHandler(db, new NullBookingEventPublisher(), NullLogger<ConfirmBookingHandler>.Instance);

        var result = await handler.Handle(new ConfirmBookingCommand(booking.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task ConfirmBookingHandler_Completed_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.StartedAtUtc = DateTimeOffset.UtcNow;
        booking.CompletedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new ConfirmBookingHandler(db, new NullBookingEventPublisher(), NullLogger<ConfirmBookingHandler>.Instance);

        var result = await handler.Handle(new ConfirmBookingCommand(booking.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task ConfirmBookingHandler_Cancelled_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.CancelledAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new ConfirmBookingHandler(db, new NullBookingEventPublisher(), NullLogger<ConfirmBookingHandler>.Instance);

        var result = await handler.Handle(new ConfirmBookingCommand(booking.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task ConfirmBookingHandler_NoShow_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.NoShowAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new ConfirmBookingHandler(db, new NullBookingEventPublisher(), NullLogger<ConfirmBookingHandler>.Instance);

        var result = await handler.Handle(new ConfirmBookingCommand(booking.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task ConfirmBookingHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new ConfirmBookingHandler(db, new NullBookingEventPublisher(), NullLogger<ConfirmBookingHandler>.Instance);

        var result = await handler.Handle(new ConfirmBookingCommand(Guid.NewGuid()));

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }

    // ==================== B10: Start Booking ====================

    [Fact]
    public async Task StartBookingHandler_FromScheduled_SetsStartedAtUtc()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        var handler = new StartBookingHandler(db);

        var result = await handler.Handle(new StartBookingCommand(booking.Id));

        Assert.True(result.IsT0);
        Assert.IsType<Success>(result.AsT0);
        var updated = await db.Bookings.FindAsync(booking.Id);
        Assert.NotNull(updated!.StartedAtUtc);
    }

    [Fact]
    public async Task StartBookingHandler_FromConfirmed_SetsStartedAtUtc()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.ConfirmedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new StartBookingHandler(db);

        var result = await handler.Handle(new StartBookingCommand(booking.Id));

        Assert.True(result.IsT0);
    }

    [Fact]
    public async Task StartBookingHandler_AlreadyStarted_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.StartedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new StartBookingHandler(db);

        var result = await handler.Handle(new StartBookingCommand(booking.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task StartBookingHandler_Completed_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.StartedAtUtc = DateTimeOffset.UtcNow;
        booking.CompletedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new StartBookingHandler(db);

        var result = await handler.Handle(new StartBookingCommand(booking.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task StartBookingHandler_Cancelled_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.CancelledAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new StartBookingHandler(db);

        var result = await handler.Handle(new StartBookingCommand(booking.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task StartBookingHandler_NoShow_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.NoShowAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new StartBookingHandler(db);

        var result = await handler.Handle(new StartBookingCommand(booking.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task StartBookingHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new StartBookingHandler(db);

        var result = await handler.Handle(new StartBookingCommand(Guid.NewGuid()));

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }

    // ==================== B11: Complete Booking ====================

    [Fact]
    public async Task CompleteBookingHandler_FromInProgress_SetsCompletedAtUtc()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.StartedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new CompleteBookingHandler(db);

        var result = await handler.Handle(new CompleteBookingCommand(booking.Id));

        Assert.True(result.IsT0);
        Assert.IsType<Success>(result.AsT0);
        var updated = await db.Bookings.FindAsync(booking.Id);
        Assert.NotNull(updated!.CompletedAtUtc);
    }

    [Fact]
    public async Task CompleteBookingHandler_FromScheduled_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        var handler = new CompleteBookingHandler(db);

        var result = await handler.Handle(new CompleteBookingCommand(booking.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task CompleteBookingHandler_FromConfirmed_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.ConfirmedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new CompleteBookingHandler(db);

        var result = await handler.Handle(new CompleteBookingCommand(booking.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task CompleteBookingHandler_AlreadyCompleted_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.StartedAtUtc = DateTimeOffset.UtcNow;
        booking.CompletedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new CompleteBookingHandler(db);

        var result = await handler.Handle(new CompleteBookingCommand(booking.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task CompleteBookingHandler_Cancelled_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.CancelledAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new CompleteBookingHandler(db);

        var result = await handler.Handle(new CompleteBookingCommand(booking.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task CompleteBookingHandler_NoShow_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.NoShowAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new CompleteBookingHandler(db);

        var result = await handler.Handle(new CompleteBookingCommand(booking.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task CompleteBookingHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new CompleteBookingHandler(db);

        var result = await handler.Handle(new CompleteBookingCommand(Guid.NewGuid()));

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }

    // ==================== B12: No-Show Booking ====================

    [Fact]
    public async Task MarkBookingNoShowHandler_FromScheduled_SetsNoShowAtUtc()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        var handler = new MarkBookingNoShowHandler(db);

        var result = await handler.Handle(new MarkBookingNoShowCommand(booking.Id));

        Assert.True(result.IsT0);
        Assert.IsType<Success>(result.AsT0);
        var updated = await db.Bookings.FindAsync(booking.Id);
        Assert.NotNull(updated!.NoShowAtUtc);
    }

    [Fact]
    public async Task MarkBookingNoShowHandler_FromConfirmed_SetsNoShowAtUtc()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.ConfirmedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new MarkBookingNoShowHandler(db);

        var result = await handler.Handle(new MarkBookingNoShowCommand(booking.Id));

        Assert.True(result.IsT0);
    }

    [Fact]
    public async Task MarkBookingNoShowHandler_InProgress_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.StartedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new MarkBookingNoShowHandler(db);

        var result = await handler.Handle(new MarkBookingNoShowCommand(booking.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task MarkBookingNoShowHandler_Completed_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.StartedAtUtc = DateTimeOffset.UtcNow;
        booking.CompletedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new MarkBookingNoShowHandler(db);

        var result = await handler.Handle(new MarkBookingNoShowCommand(booking.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task MarkBookingNoShowHandler_Cancelled_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.CancelledAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new MarkBookingNoShowHandler(db);

        var result = await handler.Handle(new MarkBookingNoShowCommand(booking.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task MarkBookingNoShowHandler_AlreadyNoShow_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.NoShowAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new MarkBookingNoShowHandler(db);

        var result = await handler.Handle(new MarkBookingNoShowCommand(booking.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task MarkBookingNoShowHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new MarkBookingNoShowHandler(db);

        var result = await handler.Handle(new MarkBookingNoShowCommand(Guid.NewGuid()));

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }

    // ==================== B2: Booking Event Publishing ====================

    [Fact]
    public async Task CreateBookingHandler_OnSuccess_CallsPublishCreatedAsync()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db);
        var publisher = new RecordingBookingEventPublisher();
        var handler = new CreateBookingHandler(db, publisher, NullLogger<CreateBookingHandler>.Instance);
        var startTime = DateTimeOffset.UtcNow.AddHours(1);

        var command = new CreateBookingCommand
        {
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = startTime,
            ServiceIds = [service.Id],
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT0);
        Assert.Single(publisher.CreatedEvents);
        Assert.Equal(client.Id, publisher.CreatedEvents[0].ClientId);
        Assert.Equal(startTime, publisher.CreatedEvents[0].StartTime);
    }

    [Fact]
    public async Task CreateBookingHandler_OnValidationError_DoesNotCallPublisher()
    {
        await using var db = CreateDbContext();
        var publisher = new RecordingBookingEventPublisher();
        var handler = new CreateBookingHandler(db, publisher, NullLogger<CreateBookingHandler>.Instance);

        var command = new CreateBookingCommand
        {
            ClientId = Guid.NewGuid(),
            StaffMemberId = Guid.NewGuid(),
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [Guid.NewGuid()],
        };

        var result = await handler.Handle(command);

        Assert.False(result.IsT0);
        Assert.Empty(publisher.CreatedEvents);
    }

    [Fact]
    public async Task ConfirmBookingHandler_OnSuccess_CallsPublishConfirmedAsync()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        var publisher = new RecordingBookingEventPublisher();
        var handler = new ConfirmBookingHandler(db, publisher, NullLogger<ConfirmBookingHandler>.Instance);

        var result = await handler.Handle(new ConfirmBookingCommand(booking.Id));

        Assert.True(result.IsT0);
        Assert.Single(publisher.ConfirmedEvents);
        Assert.Equal(booking.Id, publisher.ConfirmedEvents[0].BookingId);
        Assert.Equal(client.Id, publisher.ConfirmedEvents[0].ClientId);
    }

    [Fact]
    public async Task ConfirmBookingHandler_OnConflict_DoesNotCallPublisher()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.ConfirmedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var publisher = new RecordingBookingEventPublisher();
        var handler = new ConfirmBookingHandler(db, publisher, NullLogger<ConfirmBookingHandler>.Instance);

        var result = await handler.Handle(new ConfirmBookingCommand(booking.Id));

        Assert.True(result.IsT2);
        Assert.Empty(publisher.ConfirmedEvents);
    }

    [Fact]
    public async Task CancelBookingHandler_OnSuccess_CallsPublishCancelledAsync()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        var publisher = new RecordingBookingEventPublisher();
        var handler = new CancelBookingHandler(db, publisher, NullLogger<CancelBookingHandler>.Instance);

        var result = await handler.Handle(new CancelBookingCommand(booking.Id));

        Assert.True(result.IsT0);
        Assert.Single(publisher.CancelledEvents);
        Assert.Equal(booking.Id, publisher.CancelledEvents[0].BookingId);
        Assert.Equal(client.Id, publisher.CancelledEvents[0].ClientId);
    }

    [Fact]
    public async Task CancelBookingHandler_OnConflict_DoesNotCallPublisher()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var booking = CreateTestBooking(db, client.Id, staff.Id);
        booking.CancelledAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var publisher = new RecordingBookingEventPublisher();
        var handler = new CancelBookingHandler(db, publisher, NullLogger<CancelBookingHandler>.Instance);

        var result = await handler.Handle(new CancelBookingCommand(booking.Id));

        Assert.True(result.IsT2);
        Assert.Empty(publisher.CancelledEvents);
    }
}
