using Chairly.Api.Features.Staff.GetStaffList;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Domain.Enums;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Tests.Features.Staff;

public class StaffHandlerTests
{
    private static ChairlyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ChairlyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ChairlyDbContext(options);
    }

    private static StaffMember CreateTestStaffMember(ChairlyDbContext db, string firstName = "Jan", string lastName = "Jansen", StaffRole role = StaffRole.StaffMember)
    {
        var member = new StaffMember
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            Color = "#FF5733",
            ScheduleJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
        };
        db.StaffMembers.Add(member);
        db.SaveChanges();
        return member;
    }

    [Fact]
    public async Task GetStaffListHandler_HappyPath_ReturnsListOrderedByFirstNameLastName()
    {
        await using var db = CreateDbContext();
        CreateTestStaffMember(db, firstName: "Zara", lastName: "Bakker");
        CreateTestStaffMember(db, firstName: "Anna", lastName: "Visser");

        var handler = new GetStaffListHandler(db);
        var result = (await handler.Handle(new GetStaffListQuery())).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Anna", result[0].FirstName);
        Assert.Equal("Zara", result[1].FirstName);
    }
}
