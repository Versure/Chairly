using System.ComponentModel.DataAnnotations;
using Chairly.Api.Features.Clients.CreateClient;
using Chairly.Api.Features.Clients.DeleteClient;
using Chairly.Api.Features.Clients.GetClientsList;
using Chairly.Api.Features.Clients.UpdateClient;
using Chairly.Api.Shared.Results;
using Chairly.Api.Shared.Tenancy;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;

namespace Chairly.Tests.Features.Clients;

public class ClientHandlerTests
{
    private static ChairlyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ChairlyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ChairlyDbContext(options);
    }

    private static Client CreateTestClient(ChairlyDbContext db, string firstName = "Anna", string lastName = "Bakker", bool deleted = false)
    {
        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = TenantConstants.DefaultTenantId,
            FirstName = firstName,
            LastName = lastName,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            DeletedAtUtc = deleted ? DateTimeOffset.UtcNow.AddDays(-1) : null,
            DeletedBy = deleted ? Guid.Empty : null,
        };
        db.Clients.Add(client);
        db.SaveChanges();
        return client;
    }

    // CLB-002
    [Fact]
    public async Task GetClientsListHandler_ReturnsOnlyNonDeletedClientsForTenant()
    {
        await using var db = CreateDbContext();
        CreateTestClient(db, firstName: "Active", lastName: "Client");
        CreateTestClient(db, firstName: "Deleted", lastName: "Client", deleted: true);

        var handler = new GetClientsListHandler(db);
        var result = (await handler.Handle(new GetClientsListQuery())).ToList();

        Assert.Single(result);
        Assert.Equal("Active", result[0].FirstName);
    }

    // CLB-003
    [Fact]
    public async Task CreateClientHandler_HappyPath_CreatesAndReturnsClient()
    {
        await using var db = CreateDbContext();
        var handler = new CreateClientHandler(db);
        var command = new CreateClientCommand
        {
            FirstName = "Pieter",
            LastName = "de Vries",
            Email = "pieter@example.com",
            PhoneNumber = "0612345678",
        };

        var result = await handler.Handle(command);

        Assert.Equal("Pieter", result.FirstName);
        Assert.Equal("de Vries", result.LastName);
        Assert.Equal("pieter@example.com", result.Email);
        var saved = await db.Clients.FirstAsync();
        Assert.Equal(TenantConstants.DefaultTenantId, saved.TenantId);
    }

    [Fact]
    public void CreateClientCommand_MissingFirstName_FailsValidation()
    {
        var command = new CreateClientCommand
        {
            FirstName = string.Empty,
            LastName = "de Vries",
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateClientCommand.FirstName), StringComparer.Ordinal));
    }

    // CLB-004
    [Fact]
    public async Task UpdateClientHandler_HappyPath_UpdatesAndReturnsClient()
    {
        await using var db = CreateDbContext();
        var existing = CreateTestClient(db, firstName: "Oud", lastName: "Naam");
        var handler = new UpdateClientHandler(db);
        var command = new UpdateClientCommand
        {
            Id = existing.Id,
            FirstName = "Nieuw",
            LastName = "Naam",
            Email = "nieuw@example.com",
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Equal("Nieuw", response.FirstName);
        Assert.Equal("nieuw@example.com", response.Email);
        Assert.NotNull(response.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateClientHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new UpdateClientHandler(db);
        var command = new UpdateClientCommand
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Test",
        };

        var result = await handler.Handle(command);

        Assert.IsType<NotFound>(result.AsT1);
    }

    // CLB-005
    [Fact]
    public async Task DeleteClientHandler_HappyPath_SetsDeletedAtUtc()
    {
        await using var db = CreateDbContext();
        var existing = CreateTestClient(db);
        var handler = new DeleteClientHandler(db);

        var result = await handler.Handle(new DeleteClientCommand(existing.Id));

        Assert.IsType<Success>(result.AsT0);
        var saved = await db.Clients.FirstAsync();
        Assert.NotNull(saved.DeletedAtUtc);
    }

    [Fact]
    public async Task DeleteClientHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new DeleteClientHandler(db);

        var result = await handler.Handle(new DeleteClientCommand(Guid.NewGuid()));

        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task DeleteClientHandler_AlreadyDeleted_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var originalDeletedAt = DateTimeOffset.UtcNow.AddDays(-1);
        var existing = CreateTestClient(db, deleted: false);
        existing.DeletedAtUtc = originalDeletedAt;
        existing.DeletedBy = Guid.Empty;
        await db.SaveChangesAsync();

        var handler = new DeleteClientHandler(db);
        var result = await handler.Handle(new DeleteClientCommand(existing.Id));

        Assert.IsType<Conflict>(result.AsT2);
        var saved = await db.Clients.FirstAsync();
        Assert.Equal(originalDeletedAt, saved.DeletedAtUtc);
    }
}
