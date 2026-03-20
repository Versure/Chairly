using System.ComponentModel.DataAnnotations;
using Chairly.Api.Features.Staff.CreateStaffMember;
using Chairly.Api.Features.Staff.DeactivateStaffMember;
using Chairly.Api.Features.Staff.GetStaffList;
using Chairly.Api.Features.Staff.ReactivateStaffMember;
using Chairly.Api.Features.Staff.UpdateStaffMember;
using Chairly.Api.Shared.Results;
using Chairly.Domain.Entities;
using Chairly.Domain.Enums;
using Chairly.Infrastructure.Persistence;
using Chairly.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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
            TenantId = TestTenantContext.DefaultTenantId,
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName}@example.com",
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

        var handler = new GetStaffListHandler(db, TestTenantContext.Create());
        var result = (await handler.Handle(new GetStaffListQuery())).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Anna", result[0].FirstName);
        Assert.Equal("Zara", result[1].FirstName);
    }

    [Fact]
    public async Task CreateStaffMemberHandler_HappyPath_CreatesWithCorrectRole()
    {
        await using var db = CreateDbContext();
        var keycloak = new NullKeycloakAdminService();
        var handler = new CreateStaffMemberHandler(db, keycloak, NullLogger<CreateStaffMemberHandler>.Instance, TestTenantContext.Create());
        var command = new CreateStaffMemberCommand
        {
            FirstName = "Pieter",
            LastName = "de Vries",
            Email = "pieter@example.com",
            Role = "manager",
            Color = "#123456",
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Equal(StaffRole.Manager, (await db.StaffMembers.FirstAsync()).Role);
        Assert.Equal("Pieter", response.FirstName);
    }

    [Fact]
    public async Task CreateStaffMemberHandler_HappyPath_StoresScheduleJson()
    {
        await using var db = CreateDbContext();
        var keycloak = new NullKeycloakAdminService();
        var handler = new CreateStaffMemberHandler(db, keycloak, NullLogger<CreateStaffMemberHandler>.Instance, TestTenantContext.Create());
        var command = new CreateStaffMemberCommand
        {
            FirstName = "Sophie",
            LastName = "Bakker",
            Email = "sophie@example.com",
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
    public async Task CreateStaffMemberHandler_HappyPath_CallsKeycloakAndSetsKeycloakUserId()
    {
        await using var db = CreateDbContext();
        var keycloak = new NullKeycloakAdminService();
        var handler = new CreateStaffMemberHandler(db, keycloak, NullLogger<CreateStaffMemberHandler>.Instance, TestTenantContext.Create());
        var command = new CreateStaffMemberCommand
        {
            FirstName = "Pieter",
            LastName = "de Vries",
            Email = "pieter@example.com",
            Role = "manager",
            Color = "#123456",
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT0);
        Assert.True(keycloak.CreateUserCalled);
        Assert.True(keycloak.AssignRoleCalled);

        var saved = await db.StaffMembers.FirstAsync();
        Assert.Equal(keycloak.LastCreatedUserId, saved.KeycloakUserId);
    }

    [Fact]
    public async Task CreateStaffMemberHandler_HappyPath_SendsPasswordSetupEmail()
    {
        await using var db = CreateDbContext();
        var keycloak = new NullKeycloakAdminService();
        var handler = new CreateStaffMemberHandler(db, keycloak, NullLogger<CreateStaffMemberHandler>.Instance, TestTenantContext.Create());
        var command = new CreateStaffMemberCommand
        {
            FirstName = "Pieter",
            LastName = "de Vries",
            Email = "pieter@example.com",
            Role = "manager",
            Color = "#123456",
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT0);
        Assert.True(keycloak.SendActionsEmailCalled);
    }

    [Fact]
    public async Task CreateStaffMemberHandler_EmailSendFails_StillCreatesStaffMember()
    {
        await using var db = CreateDbContext();
        var keycloak = new SendEmailFailingKeycloakAdminService();
        var handler = new CreateStaffMemberHandler(db, keycloak, NullLogger<CreateStaffMemberHandler>.Instance, TestTenantContext.Create());
        var command = new CreateStaffMemberCommand
        {
            FirstName = "Pieter",
            LastName = "de Vries",
            Email = "pieter@example.com",
            Role = "manager",
            Color = "#123456",
        };

        var result = await handler.Handle(command);

        // Staff member should still be created even if email fails.
        Assert.True(result.IsT0);
        var saved = await db.StaffMembers.FirstAsync();
        Assert.NotNull(saved.KeycloakUserId);
    }

    [Fact]
    public async Task CreateStaffMemberHandler_KeycloakFails_DeletesDbRecordAndReturnsError()
    {
        await using var db = CreateDbContext();
        var keycloak = new FailingKeycloakAdminService();
        var handler = new CreateStaffMemberHandler(db, keycloak, NullLogger<CreateStaffMemberHandler>.Instance, TestTenantContext.Create());
        var command = new CreateStaffMemberCommand
        {
            FirstName = "Pieter",
            LastName = "de Vries",
            Email = "pieter@example.com",
            Role = "manager",
            Color = "#123456",
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
        Assert.IsType<KeycloakError>(result.AsT1);
        Assert.Empty(await db.StaffMembers.ToListAsync());
    }

    [Fact]
    public async Task CreateStaffMemberHandler_AssignRoleFails_DeletesCreatedKeycloakUserAndDbRecord()
    {
        await using var db = CreateDbContext();
        var keycloak = new CreateUserThenFailAssignRoleKeycloakAdminService();
        var handler = new CreateStaffMemberHandler(db, keycloak, NullLogger<CreateStaffMemberHandler>.Instance, TestTenantContext.Create());
        var command = new CreateStaffMemberCommand
        {
            FirstName = "Pieter",
            LastName = "de Vries",
            Email = "pieter@example.com",
            Role = "manager",
            Color = "#123456",
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
        Assert.True(keycloak.DeleteUserCalled);
        Assert.Equal(keycloak.CreatedUserId, keycloak.DeletedUserId);
        Assert.Empty(await db.StaffMembers.ToListAsync());
    }

    [Fact]
    public void CreateStaffMemberCommand_EmptyFirstName_FailsValidation()
    {
        var command = new CreateStaffMemberCommand
        {
            FirstName = string.Empty,
            LastName = "Bakker",
            Email = "test@example.com",
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
            Email = "jan@example.com",
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
    public void CreateStaffMemberCommand_MissingEmail_FailsValidation()
    {
        var command = new CreateStaffMemberCommand
        {
            FirstName = "Jan",
            LastName = "Bakker",
            Email = string.Empty,
            Role = "manager",
            Color = "#FF0000",
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateStaffMemberCommand.Email), StringComparer.Ordinal));
    }

    [Fact]
    public void CreateStaffMemberCommand_InvalidEmailFormat_FailsValidation()
    {
        var command = new CreateStaffMemberCommand
        {
            FirstName = "Jan",
            LastName = "Bakker",
            Email = "not-an-email",
            Role = "manager",
            Color = "#FF0000",
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateStaffMemberCommand.Email), StringComparer.Ordinal));
    }

    [Fact]
    public async Task UpdateStaffMemberHandler_HappyPath_UpdatesFieldsAndSetsUpdatedAtUtc()
    {
        await using var db = CreateDbContext();
        var existing = CreateTestStaffMember(db, firstName: "Oud", lastName: "Naam");
        var keycloak = new NullKeycloakAdminService();
        var handler = new UpdateStaffMemberHandler(db, keycloak, NullLogger<UpdateStaffMemberHandler>.Instance, TestTenantContext.Create());
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
        var keycloak = new NullKeycloakAdminService();
        var handler = new UpdateStaffMemberHandler(db, keycloak, NullLogger<UpdateStaffMemberHandler>.Instance, TestTenantContext.Create());
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

    [Fact]
    public async Task UpdateStaffMemberHandler_NameChanged_CallsKeycloakUpdate()
    {
        await using var db = CreateDbContext();
        var existing = CreateTestStaffMember(db, firstName: "Oud", lastName: "Naam");
        existing.KeycloakUserId = "kc-user-123";
        await db.SaveChangesAsync();

        var keycloak = new NullKeycloakAdminService();
        var handler = new UpdateStaffMemberHandler(db, keycloak, NullLogger<UpdateStaffMemberHandler>.Instance, TestTenantContext.Create());
        var command = new UpdateStaffMemberCommand
        {
            Id = existing.Id,
            FirstName = "Nieuw",
            LastName = "Naam",
            Role = "staff_member",
            Color = "#FF5733",
        };

        await handler.Handle(command);

        Assert.True(keycloak.UpdateUserCalled);
    }

    [Fact]
    public async Task UpdateStaffMemberHandler_NoKeycloakUserId_SkipsKeycloakUpdate()
    {
        await using var db = CreateDbContext();
        var existing = CreateTestStaffMember(db, firstName: "Oud", lastName: "Naam");
        // KeycloakUserId is null by default.

        var keycloak = new NullKeycloakAdminService();
        var handler = new UpdateStaffMemberHandler(db, keycloak, NullLogger<UpdateStaffMemberHandler>.Instance, TestTenantContext.Create());
        var command = new UpdateStaffMemberCommand
        {
            Id = existing.Id,
            FirstName = "Nieuw",
            LastName = "Naam",
            Role = "staff_member",
            Color = "#FF5733",
        };

        await handler.Handle(command);

        Assert.False(keycloak.UpdateUserCalled);
    }

    [Fact]
    public async Task UpdateStaffMemberHandler_KeycloakFails_StillUpdatesDb()
    {
        await using var db = CreateDbContext();
        var existing = CreateTestStaffMember(db, firstName: "Oud", lastName: "Naam");
        existing.KeycloakUserId = "kc-user-123";
        await db.SaveChangesAsync();

        var keycloak = new FailingKeycloakAdminService();
        var handler = new UpdateStaffMemberHandler(db, keycloak, NullLogger<UpdateStaffMemberHandler>.Instance, TestTenantContext.Create());
        var command = new UpdateStaffMemberCommand
        {
            Id = existing.Id,
            FirstName = "Nieuw",
            LastName = "Naam",
            Role = "staff_member",
            Color = "#FF5733",
        };

        var result = await handler.Handle(command);

        // DB update should still succeed even when Keycloak fails.
        var response = result.AsT0;
        Assert.Equal("Nieuw", response.FirstName);
    }

    [Fact]
    public async Task DeactivateStaffMemberHandler_HappyPath_SetsDeactivatedAtUtc()
    {
        await using var db = CreateDbContext();
        var existing = CreateTestStaffMember(db);
        var keycloak = new NullKeycloakAdminService();
        var handler = new DeactivateStaffMemberHandler(db, keycloak, NullLogger<DeactivateStaffMemberHandler>.Instance, TestTenantContext.Create());

        var result = await handler.Handle(new DeactivateStaffMemberCommand(existing.Id));

        var response = result.AsT0;
        Assert.False(response.IsActive);
        var saved = await db.StaffMembers.FirstAsync();
        Assert.NotNull(saved.DeactivatedAtUtc);
    }

    [Fact]
    public async Task DeactivateStaffMemberHandler_WithKeycloakUser_CallsDisable()
    {
        await using var db = CreateDbContext();
        var existing = CreateTestStaffMember(db);
        existing.KeycloakUserId = "kc-user-456";
        await db.SaveChangesAsync();

        var keycloak = new NullKeycloakAdminService();
        var handler = new DeactivateStaffMemberHandler(db, keycloak, NullLogger<DeactivateStaffMemberHandler>.Instance, TestTenantContext.Create());

        await handler.Handle(new DeactivateStaffMemberCommand(existing.Id));

        Assert.True(keycloak.DisableUserCalled);
    }

    [Fact]
    public async Task DeactivateStaffMemberHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var keycloak = new NullKeycloakAdminService();
        var handler = new DeactivateStaffMemberHandler(db, keycloak, NullLogger<DeactivateStaffMemberHandler>.Instance, TestTenantContext.Create());

        var result = await handler.Handle(new DeactivateStaffMemberCommand(Guid.NewGuid()));

        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task DeactivateStaffMemberHandler_AlreadyInactive_DoesNotOverwriteDeactivatedAtUtc()
    {
        await using var db = CreateDbContext();
        var existing = CreateTestStaffMember(db);
        var originalDeactivatedAt = DateTimeOffset.UtcNow.AddDays(-1);
        existing.DeactivatedAtUtc = originalDeactivatedAt;
        existing.DeactivatedBy = Guid.Empty;
        await db.SaveChangesAsync();

        var keycloak = new NullKeycloakAdminService();
        var handler = new DeactivateStaffMemberHandler(db, keycloak, NullLogger<DeactivateStaffMemberHandler>.Instance, TestTenantContext.Create());
        await handler.Handle(new DeactivateStaffMemberCommand(existing.Id));

        var saved = await db.StaffMembers.FirstAsync();
        Assert.Equal(originalDeactivatedAt, saved.DeactivatedAtUtc);
    }

    [Fact]
    public async Task DeactivateStaffMemberHandler_KeycloakFails_StillDeactivatesInDb()
    {
        await using var db = CreateDbContext();
        var existing = CreateTestStaffMember(db);
        existing.KeycloakUserId = "kc-user-789";
        await db.SaveChangesAsync();

        var keycloak = new FailingKeycloakAdminService();
        var handler = new DeactivateStaffMemberHandler(db, keycloak, NullLogger<DeactivateStaffMemberHandler>.Instance, TestTenantContext.Create());

        var result = await handler.Handle(new DeactivateStaffMemberCommand(existing.Id));

        // DB should still be deactivated even when Keycloak fails.
        var response = result.AsT0;
        Assert.False(response.IsActive);
    }

    [Fact]
    public async Task ReactivateStaffMemberHandler_HappyPath_ClearsDeactivatedAtUtc()
    {
        await using var db = CreateDbContext();
        var existing = CreateTestStaffMember(db);
        existing.DeactivatedAtUtc = DateTimeOffset.UtcNow;
        existing.DeactivatedBy = Guid.Empty;
        await db.SaveChangesAsync();

        var keycloak = new NullKeycloakAdminService();
        var handler = new ReactivateStaffMemberHandler(db, keycloak, NullLogger<ReactivateStaffMemberHandler>.Instance, TestTenantContext.Create());
        var result = await handler.Handle(new ReactivateStaffMemberCommand(existing.Id));

        var response = result.AsT0;
        Assert.True(response.IsActive);
        var saved = await db.StaffMembers.FirstAsync();
        Assert.Null(saved.DeactivatedAtUtc);
    }

    [Fact]
    public async Task ReactivateStaffMemberHandler_WithKeycloakUser_CallsEnable()
    {
        await using var db = CreateDbContext();
        var existing = CreateTestStaffMember(db);
        existing.DeactivatedAtUtc = DateTimeOffset.UtcNow;
        existing.DeactivatedBy = Guid.Empty;
        existing.KeycloakUserId = "kc-user-enable";
        await db.SaveChangesAsync();

        var keycloak = new NullKeycloakAdminService();
        var handler = new ReactivateStaffMemberHandler(db, keycloak, NullLogger<ReactivateStaffMemberHandler>.Instance, TestTenantContext.Create());
        await handler.Handle(new ReactivateStaffMemberCommand(existing.Id));

        Assert.True(keycloak.EnableUserCalled);
    }

    [Fact]
    public async Task ReactivateStaffMemberHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var keycloak = new NullKeycloakAdminService();
        var handler = new ReactivateStaffMemberHandler(db, keycloak, NullLogger<ReactivateStaffMemberHandler>.Instance, TestTenantContext.Create());

        var result = await handler.Handle(new ReactivateStaffMemberCommand(Guid.NewGuid()));

        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task ReactivateStaffMemberHandler_KeycloakFails_StillReactivatesInDb()
    {
        await using var db = CreateDbContext();
        var existing = CreateTestStaffMember(db);
        existing.DeactivatedAtUtc = DateTimeOffset.UtcNow;
        existing.DeactivatedBy = Guid.Empty;
        existing.KeycloakUserId = "kc-user-fail";
        await db.SaveChangesAsync();

        var keycloak = new FailingKeycloakAdminService();
        var handler = new ReactivateStaffMemberHandler(db, keycloak, NullLogger<ReactivateStaffMemberHandler>.Instance, TestTenantContext.Create());

        var result = await handler.Handle(new ReactivateStaffMemberCommand(existing.Id));

        // DB should still be reactivated even when Keycloak fails.
        var response = result.AsT0;
        Assert.True(response.IsActive);
    }
}
