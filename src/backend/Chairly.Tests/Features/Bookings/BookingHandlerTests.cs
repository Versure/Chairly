using Chairly.Api.Features.Bookings;
using Chairly.Api.Features.Bookings.CancelBooking;
using Chairly.Api.Features.Bookings.CompleteBooking;
using Chairly.Api.Features.Bookings.ConfirmBooking;
using Chairly.Api.Features.Bookings.CreateBooking;
using Chairly.Api.Features.Bookings.GetBooking;
using Chairly.Api.Features.Bookings.GetBookingsList;
using Chairly.Api.Features.Bookings.NoShowBooking;
using Chairly.Api.Features.Bookings.StartBooking;
using Chairly.Api.Features.Bookings.UpdateBooking;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Domain.Enums;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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

    private static Booking CreateTestBooking(
        ChairlyDbContext db,
        Guid? staffMemberId = null,
        Guid? clientId = null,
        DateTimeOffset? startTime = null,
        bool cancelled = false,
        bool noShow = false,
        bool completed = false,
        bool started = false,
        bool confirmed = false)
    {
        var start = startTime ?? DateTimeOffset.UtcNow.AddHours(1);
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            ClientId = clientId ?? Guid.NewGuid(),
            StaffMemberId = staffMemberId ?? Guid.NewGuid(),
            StartTime = start,
            EndTime = start.AddHours(1),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            CancelledAtUtc = cancelled ? DateTimeOffset.UtcNow : null,
            CancelledBy = cancelled ? Guid.Empty : null,
            NoShowAtUtc = noShow ? DateTimeOffset.UtcNow : null,
            NoShowBy = noShow ? Guid.Empty : null,
            CompletedAtUtc = completed ? DateTimeOffset.UtcNow : null,
            CompletedBy = completed ? Guid.Empty : null,
            StartedAtUtc = started ? DateTimeOffset.UtcNow : null,
            StartedBy = started ? Guid.Empty : null,
            ConfirmedAtUtc = confirmed ? DateTimeOffset.UtcNow : null,
            ConfirmedBy = confirmed ? Guid.Empty : null,
            BookingServices =
            [
                new BookingService
                {
                    Id = Guid.NewGuid(),
                    ServiceId = Guid.NewGuid(),
                    ServiceName = "Knippen",
                    Duration = TimeSpan.FromHours(1),
                    Price = 25.00m,
                    SortOrder = 0,
                },
            ],
        };
        db.Bookings.Add(booking);
        db.SaveChanges();
        return booking;
    }

    private static Client CreateTestClient(ChairlyDbContext db, bool deleted = false)
    {
        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            FirstName = "Anna",
            LastName = "Bakker",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            DeletedAtUtc = deleted ? DateTimeOffset.UtcNow.AddDays(-1) : null,
            DeletedBy = deleted ? Guid.Empty : null,
        };
        db.Clients.Add(client);
        db.SaveChanges();
        return client;
    }

    private static StaffMember CreateTestStaffMember(ChairlyDbContext db, bool deactivated = false)
    {
        var staffMember = new StaffMember
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            FirstName = "Jan",
            LastName = "de Vries",
            Role = StaffRole.StaffMember,
            Color = "#FF5733",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            DeactivatedAtUtc = deactivated ? DateTimeOffset.UtcNow.AddDays(-1) : null,
            DeactivatedBy = deactivated ? Guid.Empty : null,
        };
        db.StaffMembers.Add(staffMember);
        db.SaveChanges();
        return staffMember;
    }

    private static Service CreateTestService(ChairlyDbContext db, bool active = true, TimeSpan? duration = null)
    {
        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            Name = "Knippen",
            Duration = duration ?? TimeSpan.FromMinutes(60),
            Price = 25.00m,
            IsActive = active,
            SortOrder = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
        };
        db.Services.Add(service);
        db.SaveChanges();
        return service;
    }

    // GetBookingsListHandler

    [Fact]
    public async Task GetBookingsListHandler_ReturnsOnlyBookingsForDefaultTenant()
    {
        await using var db = CreateDbContext();
        CreateTestBooking(db);

        var otherTenantBooking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            StaffMemberId = Guid.NewGuid(),
            StartTime = DateTimeOffset.UtcNow.AddHours(2),
            EndTime = DateTimeOffset.UtcNow.AddHours(3),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            BookingServices = [],
        };
        db.Bookings.Add(otherTenantBooking);
        await db.SaveChangesAsync();

        var handler = new GetBookingsListHandler(db);
        var result = await handler.Handle(new GetBookingsListQuery());

        Assert.Single(result);
    }

    [Fact]
    public async Task GetBookingsListHandler_DateFilter_ReturnsOnlyBookingsOnThatDate()
    {
        await using var db = CreateDbContext();
        var targetDate = new DateOnly(2026, 6, 15);
        var targetStart = new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);
        var otherStart = new DateTimeOffset(2026, 6, 16, 10, 0, 0, TimeSpan.Zero);

        CreateTestBooking(db, startTime: targetStart);
        CreateTestBooking(db, startTime: otherStart);

        var handler = new GetBookingsListHandler(db);
        var result = await handler.Handle(new GetBookingsListQuery { Date = targetDate });

        Assert.Single(result);
    }

    [Fact]
    public async Task GetBookingsListHandler_StaffMemberFilter_ReturnsOnlyBookingsForStaffMember()
    {
        await using var db = CreateDbContext();
        var staffId = Guid.NewGuid();
        CreateTestBooking(db, staffMemberId: staffId);
        CreateTestBooking(db);

        var handler = new GetBookingsListHandler(db);
        var result = await handler.Handle(new GetBookingsListQuery { StaffMemberId = staffId });

        Assert.Single(result);
    }

    // GetBookingHandler

    [Fact]
    public async Task GetBookingHandler_ReturnsBookingResponseWithCorrectStatus_Confirmed()
    {
        await using var db = CreateDbContext();
        var booking = CreateTestBooking(db, confirmed: true);

        var handler = new GetBookingHandler(db);
        var result = await handler.Handle(new GetBookingQuery(booking.Id));

        var response = Assert.IsType<BookingResponse>(result.AsT0);
        Assert.Equal("Confirmed", response.Status);
    }

    [Fact]
    public async Task GetBookingHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();

        var handler = new GetBookingHandler(db);
        var result = await handler.Handle(new GetBookingQuery(Guid.NewGuid()));

        Assert.IsType<NotFound>(result.AsT1);
    }

    // CreateBookingHandler

    [Fact]
    public async Task CreateBookingHandler_HappyPath_CreatesAndReturnsBooking()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db);

        var handler = new CreateBookingHandler(db);
        var result = await handler.Handle(new CreateBookingCommand
        {
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [service.Id],
            Notes = "Test booking",
        });

        var response = Assert.IsType<BookingResponse>(result.AsT0);
        Assert.Equal(client.Id, response.ClientId);
        Assert.Equal(staff.Id, response.StaffMemberId);
        Assert.Single(response.Services);
        Assert.Equal(service.Name, response.Services.First().ServiceName);
        Assert.Equal(service.Price, response.Services.First().Price);

        var saved = await db.Bookings.FirstAsync();
        Assert.Equal(TenantConstants.DefaultTenantId, saved.TenantId);
        Assert.NotEqual(default, saved.CreatedAtUtc);
    }

    [Fact]
    public async Task CreateBookingHandler_NotFound_WhenClientDoesNotExist()
    {
        await using var db = CreateDbContext();
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db);

        var handler = new CreateBookingHandler(db);
        var result = await handler.Handle(new CreateBookingCommand
        {
            ClientId = Guid.NewGuid(),
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [service.Id],
        });

        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task CreateBookingHandler_NotFound_WhenClientIsDeleted()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db, deleted: true);
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db);

        var handler = new CreateBookingHandler(db);
        var result = await handler.Handle(new CreateBookingCommand
        {
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [service.Id],
        });

        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task CreateBookingHandler_NotFound_WhenStaffMemberDoesNotExist()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var service = CreateTestService(db);

        var handler = new CreateBookingHandler(db);
        var result = await handler.Handle(new CreateBookingCommand
        {
            ClientId = client.Id,
            StaffMemberId = Guid.NewGuid(),
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [service.Id],
        });

        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task CreateBookingHandler_NotFound_WhenStaffMemberIsDeactivated()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db, deactivated: true);
        var service = CreateTestService(db);

        var handler = new CreateBookingHandler(db);
        var result = await handler.Handle(new CreateBookingCommand
        {
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [service.Id],
        });

        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task CreateBookingHandler_NotFound_WhenServiceIsInactive()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db, active: false);

        var handler = new CreateBookingHandler(db);
        var result = await handler.Handle(new CreateBookingCommand
        {
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [service.Id],
        });

        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task CreateBookingHandler_ThrowsValidationException_WhenServiceIdsIsEmpty()
    {
        await using var db = CreateDbContext();

        var handler = new CreateBookingHandler(db);

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(new CreateBookingCommand
        {
            ClientId = Guid.NewGuid(),
            StaffMemberId = Guid.NewGuid(),
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [],
        }));
    }

    [Fact]
    public async Task CreateBookingHandler_Conflict_WhenOverlappingBookingExists()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db);
        var startTime = new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);

        CreateTestBooking(db, staffMemberId: staff.Id, startTime: startTime);

        var handler = new CreateBookingHandler(db);
        var result = await handler.Handle(new CreateBookingCommand
        {
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = startTime.AddMinutes(30),
            ServiceIds = [service.Id],
        });

        Assert.IsType<Conflict>(result.AsT2);
    }

    [Fact]
    public async Task CreateBookingHandler_NoConflict_WhenOverlappingBookingIsCancelled()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db);
        var startTime = new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);

        CreateTestBooking(db, staffMemberId: staff.Id, startTime: startTime, cancelled: true);

        var handler = new CreateBookingHandler(db);
        var result = await handler.Handle(new CreateBookingCommand
        {
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = startTime.AddMinutes(30),
            ServiceIds = [service.Id],
        });

        Assert.IsType<BookingResponse>(result.AsT0);
    }

    // UpdateBookingHandler

    [Fact]
    public async Task UpdateBookingHandler_Conflict_WhenBookingIsInTerminalState()
    {
        await using var db = CreateDbContext();
        var booking = CreateTestBooking(db, cancelled: true);

        var handler = new UpdateBookingHandler(db);
        var result = await handler.Handle(new UpdateBookingCommand
        {
            Id = booking.Id,
            ClientId = Guid.NewGuid(),
            StaffMemberId = Guid.NewGuid(),
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            ServiceIds = [Guid.NewGuid()],
        });

        Assert.IsType<Conflict>(result.AsT2);
    }

    [Fact(Skip = "InMemory provider cannot handle OwnsMany + Add + SaveChanges — needs PostgreSQL integration test. TODO: Cover with Testcontainers or Aspire-based integration test.")]
    public async Task UpdateBookingHandler_HappyPath_UpdatesBookingAndRecalculatesEndTime()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db, duration: TimeSpan.FromMinutes(30));

        // Seed booking WITHOUT BookingServices to avoid InMemory OwnsMany delete bug
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            EndTime = DateTimeOffset.UtcNow.AddHours(2),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            BookingServices = [],
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var handler = new UpdateBookingHandler(db);
        var newStart = new DateTimeOffset(2026, 7, 1, 14, 0, 0, TimeSpan.Zero);

        var result = await handler.Handle(new UpdateBookingCommand
        {
            Id = booking.Id,
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = newStart,
            ServiceIds = [service.Id],
            Notes = "Updated",
        });

        var response = Assert.IsType<BookingResponse>(result.AsT0);
        Assert.Equal(newStart, response.StartTime);
        Assert.Equal(newStart.AddMinutes(30), response.EndTime);
        Assert.Equal("Updated", response.Notes);
    }

    [Fact(Skip = "InMemory provider cannot handle OwnsMany + Add + SaveChanges — needs PostgreSQL integration test. TODO: Cover with Testcontainers or Aspire-based integration test.")]
    public async Task UpdateBookingHandler_OverlapExcludesSelf_NoConflict()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staff = CreateTestStaffMember(db);
        var service = CreateTestService(db);
        var startTime = new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);

        // Seed booking WITHOUT BookingServices to avoid InMemory OwnsMany delete bug
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = startTime,
            EndTime = startTime.AddHours(1),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            BookingServices = [],
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var handler = new UpdateBookingHandler(db);

        // Update to same time slot — should not conflict with itself
        var result = await handler.Handle(new UpdateBookingCommand
        {
            Id = booking.Id,
            ClientId = client.Id,
            StaffMemberId = staff.Id,
            StartTime = startTime,
            ServiceIds = [service.Id],
        });

        Assert.IsType<BookingResponse>(result.AsT0);
    }

    // CancelBookingHandler

    [Fact]
    public async Task CancelBookingHandler_HappyPath_SetsCancelledAtUtc()
    {
        await using var db = CreateDbContext();
        var booking = CreateTestBooking(db);

        var handler = new CancelBookingHandler(db);
        var result = await handler.Handle(new CancelBookingCommand(booking.Id));

        Assert.IsType<Success>(result.AsT0);
        var saved = await db.Bookings.FirstAsync();
        Assert.NotNull(saved.CancelledAtUtc);
    }

    [Fact]
    public async Task CancelBookingHandler_AlreadyCancelled_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var booking = CreateTestBooking(db, cancelled: true);

        var handler = new CancelBookingHandler(db);
        var result = await handler.Handle(new CancelBookingCommand(booking.Id));

        Assert.IsType<Conflict>(result.AsT2);
    }

    [Fact]
    public async Task CancelBookingHandler_InProgressBooking_CanBeCancelled()
    {
        await using var db = CreateDbContext();
        var booking = CreateTestBooking(db, started: true);

        var handler = new CancelBookingHandler(db);
        var result = await handler.Handle(new CancelBookingCommand(booking.Id));

        Assert.IsType<Success>(result.AsT0);
        var saved = await db.Bookings.FirstAsync();
        Assert.NotNull(saved.CancelledAtUtc);
    }

    [Fact]
    public async Task CancelBookingHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();

        var handler = new CancelBookingHandler(db);
        var result = await handler.Handle(new CancelBookingCommand(Guid.NewGuid()));

        Assert.IsType<NotFound>(result.AsT1);
    }

    // ConfirmBookingHandler

    [Fact]
    public async Task ConfirmBookingHandler_HappyPath_SetsConfirmedAtUtc()
    {
        await using var db = CreateDbContext();
        var booking = CreateTestBooking(db);

        var handler = new ConfirmBookingHandler(db);
        var result = await handler.Handle(new ConfirmBookingCommand(booking.Id));

        Assert.IsType<Success>(result.AsT0);
        var saved = await db.Bookings.FirstAsync();
        Assert.NotNull(saved.ConfirmedAtUtc);
    }

    [Fact]
    public async Task ConfirmBookingHandler_AlreadyConfirmed_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var booking = CreateTestBooking(db, confirmed: true);

        var handler = new ConfirmBookingHandler(db);
        var result = await handler.Handle(new ConfirmBookingCommand(booking.Id));

        Assert.IsType<Conflict>(result.AsT2);
    }

    // StartBookingHandler

    [Fact]
    public async Task StartBookingHandler_HappyPath_SetsStartedAtUtcFromConfirmedState()
    {
        await using var db = CreateDbContext();
        var booking = CreateTestBooking(db, confirmed: true);

        var handler = new StartBookingHandler(db);
        var result = await handler.Handle(new StartBookingCommand(booking.Id));

        Assert.IsType<Success>(result.AsT0);
        var saved = await db.Bookings.FirstAsync();
        Assert.NotNull(saved.StartedAtUtc);
    }

    // CompleteBookingHandler

    [Fact]
    public async Task CompleteBookingHandler_HappyPath_SetsCompletedAtUtcFromInProgressState()
    {
        await using var db = CreateDbContext();
        var booking = CreateTestBooking(db, started: true);

        var handler = new CompleteBookingHandler(db);
        var result = await handler.Handle(new CompleteBookingCommand(booking.Id));

        Assert.IsType<Success>(result.AsT0);
        var saved = await db.Bookings.FirstAsync();
        Assert.NotNull(saved.CompletedAtUtc);
    }

    [Fact]
    public async Task CompleteBookingHandler_Conflict_WhenBookingIsInScheduledState()
    {
        await using var db = CreateDbContext();
        var booking = CreateTestBooking(db);

        var handler = new CompleteBookingHandler(db);
        var result = await handler.Handle(new CompleteBookingCommand(booking.Id));

        Assert.IsType<Conflict>(result.AsT2);
    }

    // NoShowBookingHandler

    [Fact]
    public async Task NoShowBookingHandler_HappyPath_SetsNoShowAtUtcFromScheduledState()
    {
        await using var db = CreateDbContext();
        var booking = CreateTestBooking(db);

        var handler = new NoShowBookingHandler(db);
        var result = await handler.Handle(new NoShowBookingCommand(booking.Id));

        Assert.IsType<Success>(result.AsT0);
        var saved = await db.Bookings.FirstAsync();
        Assert.NotNull(saved.NoShowAtUtc);
    }
}
