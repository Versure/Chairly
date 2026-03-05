using System.ComponentModel.DataAnnotations;
using Chairly.Api.Features.Staff.CreateStaffMember;
using Chairly.Api.Features.Staff.GetStaffList;
using Chairly.Api.Features.Staff.UpdateStaffMember;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Domain.Enums;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;

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

    [Fact]
    public async Task CreateStaffMemberHandler_HappyPath_CreatesWithCorrectRole()
    {
        await using var db = CreateDbContext();
        var handler = new CreateStaffMemberHandler(db);
        var command = new CreateStaffMemberCommand
        {
            FirstName = "Pieter",
            LastName = "de Vries",
            Role = "manager",
            Color = "#123456",
        };

        await handler.Handle(command);

        var saved = await db.StaffMembers.FirstAsync();
        Assert.Equal(StaffRole.Manager, saved.Role);
        Assert.Equal("Pieter", saved.FirstName);
    }

    [Fact]
    public async Task CreateStaffMemberHandler_HappyPath_StoresScheduleJson()
    {
        await using var db = CreateDbContext();
        var handler = new CreateStaffMemberHandler(db);
        var command = new CreateStaffMemberCommand
        {
            FirstName = "Sophie",
            LastName = "Bakker",
            Role = "staff_member",
            Color = "#ABCDEF",
            Schedule = new Dictionary<string, ShiftBlockCommand[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["monday"] = [new ShiftBlockCommand("09:00", "17:00")],
            },
        };

        await handler.Handle(command);

        var saved = await db.StaffMembers.FirstAsync();
        Assert.NotEqual("{}", saved.ScheduleJson);
    }

    [Fact]
    public void CreateStaffMemberCommand_EmptyFirstName_FailsValidation()
    {
        var command = new CreateStaffMemberCommand
        {
            FirstName = string.Empty,
            LastName = "Bakker",
            Role = "manager",
            Color = "#FF0000",
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateStaffMemberCommand.FirstName), StringComparer.Ordinal));
    }

    [Fact]
    public void CreateStaffMemberCommand_InvalidRole_FailsValidation()
    {
        var command = new CreateStaffMemberCommand
        {
            FirstName = "Jan",
            LastName = "Bakker",
            Role = "invalid_role",
            Color = "#FF0000",
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateStaffMemberCommand.Role), StringComparer.Ordinal));
    }

    [Fact]
    public async Task UpdateStaffMemberHandler_HappyPath_UpdatesFieldsAndSetsUpdatedAtUtc()
    {
        await using var db = CreateDbContext();
        var existing = CreateTestStaffMember(db, firstName: "Oud", lastName: "Naam");
        var handler = new UpdateStaffMemberHandler(db);
        var command = new UpdateStaffMemberCommand
        {
            Id = existing.Id,
            FirstName = "Nieuw",
            LastName = "Naam",
            Role = "manager",
            Color = "#AABBCC",
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Equal("Nieuw", response.FirstName);
        Assert.Equal("manager", response.Role);
        Assert.NotNull(response.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateStaffMemberHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new UpdateStaffMemberHandler(db);
        var command = new UpdateStaffMemberCommand
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Test",
            Role = "staff_member",
            Color = "#000000",
        };

        var result = await handler.Handle(command);

        Assert.IsType<NotFound>(result.AsT1);
    }
}
