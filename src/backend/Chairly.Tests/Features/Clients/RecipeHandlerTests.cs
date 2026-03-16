using System.ComponentModel.DataAnnotations;
using Chairly.Api.Features.Clients.CreateRecipe;
using Chairly.Api.Features.Clients.GetClientRecipes;
using Chairly.Api.Features.Clients.GetRecipeByBooking;
using Chairly.Api.Features.Clients.UpdateRecipe;
using Chairly.Api.Shared.Results;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;

namespace Chairly.Tests.Features.Clients;

public class RecipeHandlerTests
{
    private static ChairlyDbContext CreateDbContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ChairlyDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName ?? Guid.NewGuid().ToString())
            .Options;
        return new ChairlyDbContext(options);
    }

    private static Booking CreateTestBooking(ChairlyDbContext db, bool completed = true, Guid? staffMemberId = null)
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            ClientId = Guid.NewGuid(),
            StaffMemberId = staffMemberId ?? Guid.NewGuid(),
            StartTime = DateTimeOffset.UtcNow.AddHours(-2),
            EndTime = DateTimeOffset.UtcNow.AddHours(-1),
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            CreatedBy = Guid.Empty,
            CompletedAtUtc = completed ? DateTimeOffset.UtcNow.AddMinutes(-30) : null,
            CompletedBy = completed ? Guid.Empty : null,
        };
        db.Bookings.Add(booking);
        db.SaveChanges();
        return booking;
    }

    private static Client CreateTestClient(ChairlyDbContext db, Guid? id = null)
    {
        var client = new Client
        {
            Id = id ?? Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            FirstName = "Anna",
            LastName = "Bakker",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
        };
        db.Clients.Add(client);
        db.SaveChanges();
        return client;
    }

    private static StaffMember CreateTestStaffMember(ChairlyDbContext db, Guid? id = null)
    {
        var staffMember = new StaffMember
        {
            Id = id ?? Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            FirstName = "Pieter",
            LastName = "de Vries",
            Color = "#FF5733",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
        };
        db.StaffMembers.Add(staffMember);
        db.SaveChanges();
        return staffMember;
    }

    private static Recipe CreateTestRecipe(ChairlyDbContext db, Guid bookingId, Guid clientId, Guid staffMemberId)
    {
        var recipeId = Guid.NewGuid();
        var recipe = new Recipe
        {
            Id = recipeId,
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

    // === CreateRecipe Tests ===

    [Fact]
    public async Task CreateRecipeHandler_HappyPath_CreatesAndReturnsRecipe()
    {
        await using var db = CreateDbContext();
        var booking = CreateTestBooking(db, completed: true);
        var handler = new CreateRecipeHandler(db, TestTenantContext.Create());
        var command = new CreateRecipeCommand
        {
            BookingId = booking.Id,
            Title = "Volledige kleuring",
            Notes = "Klant wil warme tonen",
            Products =
            [
                new CreateRecipeProductItem
                {
                    Name = "Wella Illumina",
                    Brand = "Wella",
                    Quantity = "60 ml",
                    SortOrder = 0,
                },
            ],
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("Volledige kleuring", response.Title);
        Assert.Equal("Klant wil warme tonen", response.Notes);
        Assert.Equal(booking.ClientId, response.ClientId);
        Assert.Equal(booking.StaffMemberId, response.StaffMemberId);
        Assert.Single(response.Products);
        Assert.Equal("Wella Illumina", response.Products[0].Name);
        Assert.Equal(1, await db.Recipes.CountAsync());
    }

    [Fact]
    public async Task CreateRecipeHandler_BookingNotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new CreateRecipeHandler(db, TestTenantContext.Create());
        var command = new CreateRecipeCommand
        {
            BookingId = Guid.NewGuid(),
            Title = "Test",
        };

        var result = await handler.Handle(command);

        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task CreateRecipeHandler_BookingNotCompleted_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var booking = CreateTestBooking(db, completed: false);
        var handler = new CreateRecipeHandler(db, TestTenantContext.Create());
        var command = new CreateRecipeCommand
        {
            BookingId = booking.Id,
            Title = "Test",
        };

        var result = await handler.Handle(command);

        var unprocessable = result.AsT2;
        Assert.IsType<Unprocessable>(unprocessable);
        Assert.Equal("Booking is niet afgerond", unprocessable.Message);
    }

    [Fact]
    public async Task CreateRecipeHandler_RecipeAlreadyExists_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var booking = CreateTestBooking(db, completed: true);
        CreateTestRecipe(db, booking.Id, booking.ClientId, booking.StaffMemberId);
        var handler = new CreateRecipeHandler(db, TestTenantContext.Create());
        var command = new CreateRecipeCommand
        {
            BookingId = booking.Id,
            Title = "Another recipe",
        };

        var result = await handler.Handle(command);

        var conflict = result.AsT3;
        Assert.IsType<Conflict>(conflict);
        Assert.Equal("Er bestaat al een recept voor deze boeking", conflict.Message);
    }

    [Fact]
    public void CreateRecipeCommand_EmptyTitle_FailsValidation()
    {
        var command = new CreateRecipeCommand
        {
            BookingId = Guid.NewGuid(),
            Title = string.Empty,
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateRecipeCommand.Title), StringComparer.Ordinal));
    }

    [Fact]
    public void CreateRecipeCommand_TitleTooLong_FailsValidation()
    {
        var command = new CreateRecipeCommand
        {
            BookingId = Guid.NewGuid(),
            Title = new string('A', 201),
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateRecipeCommand.Title), StringComparer.Ordinal));
    }

    // === GetRecipeByBooking Tests ===

    [Fact]
    public async Task GetRecipeByBookingHandler_HappyPath_ReturnsRecipe()
    {
        await using var db = CreateDbContext();
        var booking = CreateTestBooking(db, completed: true);
        var recipe = CreateTestRecipe(db, booking.Id, booking.ClientId, booking.StaffMemberId);
        var handler = new GetRecipeByBookingHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new GetRecipeByBookingQuery(booking.Id));

        var response = result.AsT0;
        Assert.Equal(recipe.Id, response.Id);
        Assert.Equal(recipe.Title, response.Title);
        Assert.Single(response.Products);
    }

    [Fact]
    public async Task GetRecipeByBookingHandler_NoRecipeForBooking_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new GetRecipeByBookingHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new GetRecipeByBookingQuery(Guid.NewGuid()));

        Assert.IsType<NotFound>(result.AsT1);
    }

    // === UpdateRecipe Tests ===

    [Fact(Skip = "InMemory provider does not support OwnsMany update correctly — covered by PostgreSQL integration tests")]
    public async Task UpdateRecipeHandler_HappyPath_UpdatesAndReturnsRecipe()
    {
        await using var db = CreateDbContext();
        var booking = CreateTestBooking(db, completed: true);

        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            BookingId = booking.Id,
            ClientId = booking.ClientId,
            StaffMemberId = booking.StaffMemberId,
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
        await db.SaveChangesAsync();

        var handler = new UpdateRecipeHandler(db, TestTenantContext.Create());
        var command = new UpdateRecipeCommand
        {
            Id = recipe.Id,
            Title = "Bijgewerkte kleuring",
            Notes = "Nieuwe notities",
            Products =
            [
                new CreateRecipeProductItem
                {
                    Name = "L'Oreal Majirel",
                    Brand = "L'Oreal",
                    Quantity = "80 ml",
                    SortOrder = 0,
                },
                new CreateRecipeProductItem
                {
                    Name = "Oxidatie creme",
                    Quantity = "80 ml",
                    SortOrder = 1,
                },
            ],
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Equal("Bijgewerkte kleuring", response.Title);
        Assert.Equal("Nieuwe notities", response.Notes);
        Assert.NotNull(response.UpdatedAtUtc);
        Assert.Equal(2, response.Products.Count);
        Assert.Equal("L'Oreal Majirel", response.Products[0].Name);
    }

    [Fact]
    public async Task UpdateRecipeHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new UpdateRecipeHandler(db, TestTenantContext.Create());
        var command = new UpdateRecipeCommand
        {
            Id = Guid.NewGuid(),
            Title = "Test",
        };

        var result = await handler.Handle(command);

        Assert.IsType<NotFound>(result.AsT1);
    }

    // === GetClientRecipes Tests ===

    [Fact]
    public async Task GetClientRecipesHandler_EmptyList_WhenNoRecipes()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var handler = new GetClientRecipesHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new GetClientRecipesQuery(client.Id));

        var recipes = result.AsT0;
        Assert.Empty(recipes);
    }

    [Fact]
    public async Task GetClientRecipesHandler_ReturnsListOrderedNewestFirst()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staffMember = CreateTestStaffMember(db);

        var booking1 = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            ClientId = client.Id,
            StaffMemberId = staffMember.Id,
            StartTime = DateTimeOffset.UtcNow.AddDays(-7),
            EndTime = DateTimeOffset.UtcNow.AddDays(-7).AddHours(1),
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-7),
            CreatedBy = Guid.Empty,
            CompletedAtUtc = DateTimeOffset.UtcNow.AddDays(-7),
            CompletedBy = Guid.Empty,
        };
        db.Bookings.Add(booking1);

        var booking2 = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            ClientId = client.Id,
            StaffMemberId = staffMember.Id,
            StartTime = DateTimeOffset.UtcNow.AddDays(-1),
            EndTime = DateTimeOffset.UtcNow.AddDays(-1).AddHours(1),
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            CreatedBy = Guid.Empty,
            CompletedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            CompletedBy = Guid.Empty,
        };
        db.Bookings.Add(booking2);
        await db.SaveChangesAsync();

        var recipe1 = new Recipe
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            BookingId = booking1.Id,
            ClientId = client.Id,
            StaffMemberId = staffMember.Id,
            Title = "Older recipe",
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-7),
            CreatedBy = Guid.Empty,
        };
        db.Recipes.Add(recipe1);

        var recipe2 = new Recipe
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            BookingId = booking2.Id,
            ClientId = client.Id,
            StaffMemberId = staffMember.Id,
            Title = "Newer recipe",
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            CreatedBy = Guid.Empty,
        };
        db.Recipes.Add(recipe2);
        await db.SaveChangesAsync();

        var handler = new GetClientRecipesHandler(db, TestTenantContext.Create());
        var result = await handler.Handle(new GetClientRecipesQuery(client.Id));

        var recipes = result.AsT0;
        Assert.Equal(2, recipes.Count);
        Assert.Equal("Newer recipe", recipes[0].Title);
        Assert.Equal("Older recipe", recipes[1].Title);
    }

    [Fact]
    public async Task GetClientRecipesHandler_ClientNotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new GetClientRecipesHandler(db, TestTenantContext.Create());

        var result = await handler.Handle(new GetClientRecipesQuery(Guid.NewGuid()));

        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task GetClientRecipesHandler_IncludesStaffMemberNameAndBookingDate()
    {
        await using var db = CreateDbContext();
        var client = CreateTestClient(db);
        var staffMember = CreateTestStaffMember(db);
        var bookingStartTime = DateTimeOffset.UtcNow.AddDays(-3);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            ClientId = client.Id,
            StaffMemberId = staffMember.Id,
            StartTime = bookingStartTime,
            EndTime = bookingStartTime.AddHours(1),
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-3),
            CreatedBy = Guid.Empty,
            CompletedAtUtc = DateTimeOffset.UtcNow.AddDays(-3),
            CompletedBy = Guid.Empty,
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var recipeId = Guid.NewGuid();
        var recipe = new Recipe
        {
            Id = recipeId,
            TenantId = TestTenantContext.DefaultTenantId,
            BookingId = booking.Id,
            ClientId = client.Id,
            StaffMemberId = staffMember.Id,
            Title = "Knippen en kleuren",
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
        await db.SaveChangesAsync();

        var handler = new GetClientRecipesHandler(db, TestTenantContext.Create());
        var result = await handler.Handle(new GetClientRecipesQuery(client.Id));

        var recipes = result.AsT0;
        Assert.Single(recipes);
        Assert.Equal("Pieter de Vries", recipes[0].StaffMemberName);
        Assert.Equal(bookingStartTime, recipes[0].BookingDate);
        Assert.Single(recipes[0].Products);
    }

    // === Authorization placeholder tests ===

    [Fact(Skip = "Authorization deferred to Keycloak integration")]
    public void CreateRecipeHandler_Returns403WhenStaffMemberTriesToAddRecipeForAnotherStaffMembersBooking()
    {
    }

    [Fact(Skip = "Authorization deferred to Keycloak integration")]
    public void GetRecipeByBookingHandler_Returns403WhenStaffMemberRequestsAnotherStaffMembersRecipe()
    {
    }

    [Fact(Skip = "Authorization deferred to Keycloak integration")]
    public void UpdateRecipeHandler_Returns403WhenStaffMemberTriesToEditAnotherStaffMembersRecipe()
    {
    }

    [Fact(Skip = "Authorization deferred to Keycloak integration")]
    public void GetClientRecipesHandler_StaffMemberOnlySeesTheirOwnRecipesForTheClient()
    {
    }
}
