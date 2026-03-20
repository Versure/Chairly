using Chairly.Domain.Entities;
using Chairly.Domain.Enums;
using Chairly.Infrastructure.Persistence;

namespace Chairly.Tests.Helpers;

/// <summary>
/// Shared entity builders for test data creation. Each method creates and persists
/// an entity with sensible defaults that can be overridden via parameters.
/// </summary>
internal static class EntityBuilders
{
    public static Client CreateClient(
        ChairlyDbContext db,
        string firstName = "Anna",
        string lastName = "Bakker",
        string? email = "anna@example.com",
        string? phoneNumber = null,
        bool deleted = false)
    {
        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = phoneNumber,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = TestTenantContext.DefaultUserId,
            DeletedAtUtc = deleted ? DateTimeOffset.UtcNow.AddDays(-1) : null,
            DeletedBy = deleted ? TestTenantContext.DefaultUserId : null,
        };
        db.Clients.Add(client);
        db.SaveChanges();
        return client;
    }

    public static StaffMember CreateStaffMember(
        ChairlyDbContext db,
        string firstName = "Jan",
        string lastName = "Jansen",
        string? email = null,
        StaffRole role = StaffRole.StaffMember,
        string color = "#6366f1",
        bool deactivated = false)
    {
        var member = new StaffMember
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            FirstName = firstName,
            LastName = lastName,
            Email = email ?? $"{firstName}@example.com",
            Role = role,
            Color = color,
            ScheduleJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = TestTenantContext.DefaultUserId,
            DeactivatedAtUtc = deactivated ? DateTimeOffset.UtcNow.AddDays(-1) : null,
            DeactivatedBy = deactivated ? TestTenantContext.DefaultUserId : null,
        };
        db.StaffMembers.Add(member);
        db.SaveChanges();
        return member;
    }

    public static ServiceCategory CreateServiceCategory(
        ChairlyDbContext db,
        string name = "Knippen",
        int sortOrder = 0)
    {
        var category = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            Name = name,
            SortOrder = sortOrder,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = TestTenantContext.DefaultUserId,
        };
        db.ServiceCategories.Add(category);
        db.SaveChanges();
        return category;
    }

    public static Service CreateService(
        ChairlyDbContext db,
        Guid? categoryId = null,
        string name = "Heren Knippen",
        TimeSpan? duration = null,
        decimal price = 25.00m)
    {
        var service = new Service
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            CategoryId = categoryId ?? Guid.NewGuid(),
            Name = name,
            Duration = duration ?? TimeSpan.FromMinutes(30),
            Price = price,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = TestTenantContext.DefaultUserId,
        };
        db.Services.Add(service);
        db.SaveChanges();
        return service;
    }

    public static Booking CreateBooking(
        ChairlyDbContext db,
        Guid? clientId = null,
        Guid? staffMemberId = null,
        DateTimeOffset? startTime = null,
        int durationMinutes = 30,
        bool confirmed = false,
        bool cancelled = false)
    {
        var start = startTime ?? DateTimeOffset.UtcNow.AddHours(1);
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            ClientId = clientId ?? Guid.NewGuid(),
            StaffMemberId = staffMemberId ?? Guid.NewGuid(),
            StartTime = start,
            EndTime = start.AddMinutes(durationMinutes),
            Notes = null,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = TestTenantContext.DefaultUserId,
            ConfirmedAtUtc = confirmed ? DateTimeOffset.UtcNow : null,
            ConfirmedBy = confirmed ? TestTenantContext.DefaultUserId : null,
            CancelledAtUtc = cancelled ? DateTimeOffset.UtcNow : null,
            CancelledBy = cancelled ? TestTenantContext.DefaultUserId : null,
        };
        db.Bookings.Add(booking);
        db.SaveChanges();
        return booking;
    }
}
