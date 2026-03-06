using Chairly.Api.Features.Bookings.CancelBooking;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
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

    // CBK-004: CancelBookingHandler

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
}
