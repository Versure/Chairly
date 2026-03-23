using System.ComponentModel.DataAnnotations;
using Chairly.Api.Features.Admin;
using Chairly.Api.Features.Admin.CancelSubscription;
using Chairly.Api.Features.Admin.GetAdminSubscription;
using Chairly.Api.Features.Admin.GetAdminSubscriptionsList;
using Chairly.Api.Features.Admin.ProvisionSubscription;
using Chairly.Api.Features.Admin.UpdateSubscriptionPlan;
using Chairly.Api.Features.Config.GetAdminConfig;
using Chairly.Domain.Entities;
using Chairly.Domain.Enums;
using Chairly.Infrastructure.Keycloak;
using Chairly.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OneOf.Types;

namespace Chairly.Tests.Features.Admin;

public class AdminSubscriptionHandlerTests
{
    private static WebsiteDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WebsiteDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new WebsiteDbContext(options);
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["Keycloak:Url"] = "http://localhost:8080",
                ["Keycloak:AdminPortalRealm"] = "chairly-admin",
                ["Keycloak:AdminPortalClientId"] = "chairly-admin-portal",
            })
            .Build();

    private static Subscription CreateTestSubscription(WebsiteDbContext db, Action<Subscription>? configure = null)
    {
        var entity = new Subscription
        {
            Id = Guid.NewGuid(),
            SalonName = "Test Salon",
            OwnerFirstName = "Jan",
            OwnerLastName = "de Vries",
            Email = "jan@testsalon.nl",
            Plan = SubscriptionPlan.Starter,
            BillingCycle = BillingCycle.Monthly,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        configure?.Invoke(entity);
        db.Subscriptions.Add(entity);
        db.SaveChanges();
        return entity;
    }

    private static HttpContextAccessor CreateMockHttpContextAccessor(Guid? userId = null)
    {
        var httpContext = new DefaultHttpContext();
        if (userId is not null)
        {
            var claims = new[] { new System.Security.Claims.Claim("sub", userId.Value.ToString()) };
            httpContext.User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(claims, "test"));
        }

        return new HttpContextAccessor { HttpContext = httpContext };
    }

    // --- GetAdminConfig ---

    [Fact]
    public async Task GetAdminConfigHandler_ReturnsConfigValues()
    {
        var config = CreateConfiguration();
        var handler = new GetAdminConfigHandler(config);

        var result = await handler.Handle(new GetAdminConfigQuery());

        Assert.Equal("http://localhost:8080", result.KeycloakUrl);
        Assert.Equal("chairly-admin", result.KeycloakRealm);
        Assert.Equal("chairly-admin-portal", result.KeycloakClientId);
    }

    // --- GetAdminSubscriptionsList ---

    [Fact]
    public async Task GetAdminSubscriptionsListHandler_ReturnsAllSubscriptions()
    {
        await using var db = CreateDbContext();
        CreateTestSubscription(db);
        CreateTestSubscription(db, s => { s.SalonName = "Other Salon"; s.Email = "other@salon.nl"; });
        var handler = new GetAdminSubscriptionsListHandler(db);

        var result = await handler.Handle(new GetAdminSubscriptionsListQuery(null, null, null, 1, 25));

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetAdminSubscriptionsListHandler_SearchBySalonName_ReturnsMatching()
    {
        await using var db = CreateDbContext();
        CreateTestSubscription(db, s => s.SalonName = "Salon Mooi");
        CreateTestSubscription(db, s => { s.SalonName = "Kapper Jan"; s.Email = "kapper@jan.nl"; });
        var handler = new GetAdminSubscriptionsListHandler(db);

        var result = await handler.Handle(new GetAdminSubscriptionsListQuery("Mooi", null, null, 1, 25));

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Salon Mooi", result.Items[0].SalonName);
    }

    [Fact]
    public async Task GetAdminSubscriptionsListHandler_SearchByEmail_ReturnsMatching()
    {
        await using var db = CreateDbContext();
        CreateTestSubscription(db, s => s.Email = "unique@example.com");
        CreateTestSubscription(db, s => { s.SalonName = "Other"; s.Email = "other@example.com"; });
        var handler = new GetAdminSubscriptionsListHandler(db);

        var result = await handler.Handle(new GetAdminSubscriptionsListQuery("unique", null, null, 1, 25));

        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetAdminSubscriptionsListHandler_FilterByStatusTrial_ReturnsTrialOnly()
    {
        await using var db = CreateDbContext();
        CreateTestSubscription(db, s => { s.TrialEndsAtUtc = DateTimeOffset.UtcNow.AddDays(30); s.BillingCycle = null; });
        CreateTestSubscription(db, s => { s.SalonName = "Paid"; s.Email = "paid@salon.nl"; s.ProvisionedAtUtc = DateTimeOffset.UtcNow; });
        var handler = new GetAdminSubscriptionsListHandler(db);

        var result = await handler.Handle(new GetAdminSubscriptionsListQuery(null, "trial", null, 1, 25));

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("trial", result.Items[0].Status);
    }

    [Fact]
    public async Task GetAdminSubscriptionsListHandler_FilterByStatusProvisioned_ReturnsProvisionedOnly()
    {
        await using var db = CreateDbContext();
        CreateTestSubscription(db, s => { s.ProvisionedAtUtc = DateTimeOffset.UtcNow; });
        CreateTestSubscription(db, s => { s.SalonName = "Pending"; s.Email = "pending@salon.nl"; });
        var handler = new GetAdminSubscriptionsListHandler(db);

        var result = await handler.Handle(new GetAdminSubscriptionsListQuery(null, "provisioned", null, 1, 25));

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("provisioned", result.Items[0].Status);
    }

    [Fact]
    public async Task GetAdminSubscriptionsListHandler_FilterByPlanStarter_ReturnsStarterOnly()
    {
        await using var db = CreateDbContext();
        CreateTestSubscription(db, s => s.Plan = SubscriptionPlan.Starter);
        CreateTestSubscription(db, s => { s.SalonName = "Team Salon"; s.Email = "team@salon.nl"; s.Plan = SubscriptionPlan.Team; });
        var handler = new GetAdminSubscriptionsListHandler(db);

        var result = await handler.Handle(new GetAdminSubscriptionsListQuery(null, null, "starter", 1, 25));

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("starter", result.Items[0].Plan);
    }

    [Fact]
    public async Task GetAdminSubscriptionsListHandler_Pagination_WorksCorrectly()
    {
        await using var db = CreateDbContext();
        for (var i = 0; i < 5; i++)
        {
            CreateTestSubscription(db, s => { s.SalonName = $"Salon {i}"; s.Email = $"s{i}@test.nl"; });
        }

        var handler = new GetAdminSubscriptionsListHandler(db);

        var page1 = await handler.Handle(new GetAdminSubscriptionsListQuery(null, null, null, 1, 2));
        var page2 = await handler.Handle(new GetAdminSubscriptionsListQuery(null, null, null, 2, 2));

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(5, page2.TotalCount);
        Assert.Equal(2, page2.Items.Count);
    }

    [Fact]
    public async Task GetAdminSubscriptionsListHandler_OrdersByCreatedAtUtcDescending()
    {
        await using var db = CreateDbContext();
        var older = CreateTestSubscription(db, s => { s.SalonName = "Older"; s.Email = "older@test.nl"; s.CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-2); });
        var newer = CreateTestSubscription(db, s => { s.SalonName = "Newer"; s.Email = "newer@test.nl"; s.CreatedAtUtc = DateTimeOffset.UtcNow; });
        var handler = new GetAdminSubscriptionsListHandler(db);

        var result = await handler.Handle(new GetAdminSubscriptionsListQuery(null, null, null, 1, 25));

        Assert.Equal(newer.Id, result.Items[0].Id);
        Assert.Equal(older.Id, result.Items[1].Id);
    }

    [Fact]
    public void GetAdminSubscriptionsListQuery_PageZero_FailsValidation()
    {
        var query = new GetAdminSubscriptionsListQuery(null, null, null, 0, 25);
        var results = new List<ValidationResult>();
        var context = new ValidationContext(query);

        var isValid = Validator.TryValidateObject(query, context, results, validateAllProperties: true);

        Assert.False(isValid);
    }

    [Fact]
    public void GetAdminSubscriptionsListQuery_PageNegative_FailsValidation()
    {
        var query = new GetAdminSubscriptionsListQuery(null, null, null, -1, 25);
        var results = new List<ValidationResult>();
        var context = new ValidationContext(query);

        var isValid = Validator.TryValidateObject(query, context, results, validateAllProperties: true);

        Assert.False(isValid);
    }

    [Fact]
    public void GetAdminSubscriptionsListQuery_PageSizeZero_FailsValidation()
    {
        var query = new GetAdminSubscriptionsListQuery(null, null, null, 1, 0);
        var results = new List<ValidationResult>();
        var context = new ValidationContext(query);

        var isValid = Validator.TryValidateObject(query, context, results, validateAllProperties: true);

        Assert.False(isValid);
    }

    [Fact]
    public void GetAdminSubscriptionsListQuery_PageSize101_FailsValidation()
    {
        var query = new GetAdminSubscriptionsListQuery(null, null, null, 1, 101);
        var results = new List<ValidationResult>();
        var context = new ValidationContext(query);

        var isValid = Validator.TryValidateObject(query, context, results, validateAllProperties: true);

        Assert.False(isValid);
    }

    // --- GetAdminSubscription ---

    [Fact]
    public async Task GetAdminSubscriptionHandler_ExistingId_ReturnsDetail()
    {
        await using var db = CreateDbContext();
        var entity = CreateTestSubscription(db);
        var handler = new GetAdminSubscriptionHandler(db, new FakeKeycloakAdminService(), CreateConfiguration());

        var result = await handler.Handle(new GetAdminSubscriptionQuery(entity.Id));

        var response = result.AsT0;
        Assert.Equal(entity.Id, response.Id);
        Assert.Equal("Test Salon", response.SalonName);
        Assert.Equal("Jan", response.OwnerFirstName);
        Assert.Equal("de Vries", response.OwnerLastName);
        Assert.Equal("starter", response.Plan);
    }

    [Fact]
    public async Task GetAdminSubscriptionHandler_NonExistentId_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new GetAdminSubscriptionHandler(db, new FakeKeycloakAdminService(), CreateConfiguration());

        var result = await handler.Handle(new GetAdminSubscriptionQuery(Guid.NewGuid()));

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }

    [Fact]
    public async Task GetAdminSubscriptionHandler_TrialSubscription_ReturnsCorrectStatusAndIsTrial()
    {
        await using var db = CreateDbContext();
        var entity = CreateTestSubscription(db, s => { s.TrialEndsAtUtc = DateTimeOffset.UtcNow.AddDays(30); s.BillingCycle = null; });
        var handler = new GetAdminSubscriptionHandler(db, new FakeKeycloakAdminService(), CreateConfiguration());

        var result = await handler.Handle(new GetAdminSubscriptionQuery(entity.Id));

        var response = result.AsT0;
        Assert.Equal("trial", response.Status);
        Assert.True(response.IsTrial);
    }

    [Fact]
    public async Task GetAdminSubscriptionHandler_WithCreatedBy_ResolvesDisplayName()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var entity = CreateTestSubscription(db, s => s.CreatedBy = userId);
        var fakeKeycloak = new FakeKeycloakAdminService();
        fakeKeycloak.SetDisplayName(userId, "Admin User");
        var handler = new GetAdminSubscriptionHandler(db, fakeKeycloak, CreateConfiguration());

        var result = await handler.Handle(new GetAdminSubscriptionQuery(entity.Id));

        var response = result.AsT0;
        Assert.Equal("Admin User", response.CreatedByName);
    }

    // --- ProvisionSubscription ---

    [Fact]
    public async Task ProvisionSubscriptionHandler_PendingSubscription_SetsProvisionedAtUtcAndByName()
    {
        await using var db = CreateDbContext();
        var entity = CreateTestSubscription(db);
        var adminId = Guid.NewGuid();
        var fakeKeycloak = new FakeKeycloakAdminService();
        fakeKeycloak.SetDisplayName(adminId, "Admin Piet");
        var handler = new ProvisionSubscriptionHandler(db, CreateMockHttpContextAccessor(adminId), fakeKeycloak, CreateConfiguration());

        var result = await handler.Handle(new ProvisionSubscriptionCommand { Id = entity.Id });

        var response = result.AsT0;
        Assert.NotNull(response.ProvisionedAtUtc);
        Assert.Equal("Admin Piet", response.ProvisionedByName);
        Assert.Equal("provisioned", response.Status);
    }

    [Fact]
    public async Task ProvisionSubscriptionHandler_AlreadyProvisioned_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var entity = CreateTestSubscription(db, s => { s.ProvisionedAtUtc = DateTimeOffset.UtcNow; s.ProvisionedBy = Guid.NewGuid(); });
        var handler = new ProvisionSubscriptionHandler(db, CreateMockHttpContextAccessor(), new FakeKeycloakAdminService(), CreateConfiguration());

        var result = await handler.Handle(new ProvisionSubscriptionCommand { Id = entity.Id });

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task ProvisionSubscriptionHandler_CancelledSubscription_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var entity = CreateTestSubscription(db, s => { s.CancelledAtUtc = DateTimeOffset.UtcNow; s.CancellationReason = "Test"; });
        var handler = new ProvisionSubscriptionHandler(db, CreateMockHttpContextAccessor(), new FakeKeycloakAdminService(), CreateConfiguration());

        var result = await handler.Handle(new ProvisionSubscriptionCommand { Id = entity.Id });

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task ProvisionSubscriptionHandler_NonExistent_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new ProvisionSubscriptionHandler(db, CreateMockHttpContextAccessor(), new FakeKeycloakAdminService(), CreateConfiguration());

        var result = await handler.Handle(new ProvisionSubscriptionCommand { Id = Guid.NewGuid() });

        Assert.True(result.IsT1);
        Assert.IsType<NotFound>(result.AsT1);
    }

    // --- CancelSubscription ---

    [Fact]
    public async Task CancelSubscriptionHandler_PendingSubscription_SetsCancelledFields()
    {
        await using var db = CreateDbContext();
        var entity = CreateTestSubscription(db);
        var adminId = Guid.NewGuid();
        var fakeKeycloak = new FakeKeycloakAdminService();
        fakeKeycloak.SetDisplayName(adminId, "Admin Karel");
        var handler = new CancelSubscriptionHandler(db, CreateMockHttpContextAccessor(adminId), fakeKeycloak, CreateConfiguration());

        var result = await handler.Handle(new CancelSubscriptionCommand { Id = entity.Id, CancellationReason = "Niet betaald" });

        var response = result.AsT0;
        Assert.NotNull(response.CancelledAtUtc);
        Assert.Equal("Admin Karel", response.CancelledByName);
        Assert.Equal("Niet betaald", response.CancellationReason);
        Assert.Equal("cancelled", response.Status);
    }

    [Fact]
    public async Task CancelSubscriptionHandler_ProvisionedSubscription_SetsCancelledFields()
    {
        await using var db = CreateDbContext();
        var entity = CreateTestSubscription(db, s => { s.ProvisionedAtUtc = DateTimeOffset.UtcNow; });
        var handler = new CancelSubscriptionHandler(db, CreateMockHttpContextAccessor(Guid.NewGuid()), new FakeKeycloakAdminService(), CreateConfiguration());

        var result = await handler.Handle(new CancelSubscriptionCommand { Id = entity.Id, CancellationReason = "Opzeggen" });

        var response = result.AsT0;
        Assert.Equal("cancelled", response.Status);
    }

    [Fact]
    public async Task CancelSubscriptionHandler_AlreadyCancelled_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var entity = CreateTestSubscription(db, s => { s.CancelledAtUtc = DateTimeOffset.UtcNow; s.CancellationReason = "Already"; });
        var handler = new CancelSubscriptionHandler(db, CreateMockHttpContextAccessor(), new FakeKeycloakAdminService(), CreateConfiguration());

        var result = await handler.Handle(new CancelSubscriptionCommand { Id = entity.Id, CancellationReason = "Again" });

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task CancelSubscriptionHandler_NonExistent_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new CancelSubscriptionHandler(db, CreateMockHttpContextAccessor(), new FakeKeycloakAdminService(), CreateConfiguration());

        var result = await handler.Handle(new CancelSubscriptionCommand { Id = Guid.NewGuid(), CancellationReason = "Test" });

        Assert.True(result.IsT1);
    }

    [Fact]
    public void CancelSubscriptionCommand_EmptyReason_FailsValidation()
    {
        var command = new CancelSubscriptionCommand { Id = Guid.NewGuid(), CancellationReason = string.Empty };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CancelSubscriptionCommand.CancellationReason), StringComparer.Ordinal));
    }

    // --- UpdateSubscriptionPlan ---

    [Fact]
    public async Task UpdateSubscriptionPlanHandler_UpdatePlan_Works()
    {
        await using var db = CreateDbContext();
        var entity = CreateTestSubscription(db);
        var handler = new UpdateSubscriptionPlanHandler(db, new FakeKeycloakAdminService(), CreateConfiguration());

        var result = await handler.Handle(new UpdateSubscriptionPlanCommand { Id = entity.Id, Plan = "team", BillingCycle = "Monthly" });

        var response = result.AsT0;
        Assert.Equal("team", response.Plan);
    }

    [Fact]
    public async Task UpdateSubscriptionPlanHandler_UpdateBillingCycle_Works()
    {
        await using var db = CreateDbContext();
        var entity = CreateTestSubscription(db);
        var handler = new UpdateSubscriptionPlanHandler(db, new FakeKeycloakAdminService(), CreateConfiguration());

        var result = await handler.Handle(new UpdateSubscriptionPlanCommand { Id = entity.Id, Plan = "starter", BillingCycle = "Annual" });

        var response = result.AsT0;
        Assert.Equal("Annual", response.BillingCycle);
    }

    [Fact]
    public async Task UpdateSubscriptionPlanHandler_UpdateBothPlanAndCycle_Works()
    {
        await using var db = CreateDbContext();
        var entity = CreateTestSubscription(db);
        var handler = new UpdateSubscriptionPlanHandler(db, new FakeKeycloakAdminService(), CreateConfiguration());

        var result = await handler.Handle(new UpdateSubscriptionPlanCommand { Id = entity.Id, Plan = "salon", BillingCycle = "Annual" });

        var response = result.AsT0;
        Assert.Equal("salon", response.Plan);
        Assert.Equal("Annual", response.BillingCycle);
    }

    [Fact]
    public async Task UpdateSubscriptionPlanHandler_TrialToPaid_ClearsTrialEndsAtUtc()
    {
        await using var db = CreateDbContext();
        var entity = CreateTestSubscription(db, s => { s.TrialEndsAtUtc = DateTimeOffset.UtcNow.AddDays(30); s.BillingCycle = null; });
        var handler = new UpdateSubscriptionPlanHandler(db, new FakeKeycloakAdminService(), CreateConfiguration());

        var result = await handler.Handle(new UpdateSubscriptionPlanCommand { Id = entity.Id, Plan = "starter", BillingCycle = "Monthly" });

        var response = result.AsT0;
        Assert.Null(response.TrialEndsAtUtc);
        Assert.False(response.IsTrial);
        Assert.Equal("Monthly", response.BillingCycle);
    }

    [Fact]
    public async Task UpdateSubscriptionPlanHandler_CancelledSubscription_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var entity = CreateTestSubscription(db, s => { s.CancelledAtUtc = DateTimeOffset.UtcNow; s.CancellationReason = "Test"; });
        var handler = new UpdateSubscriptionPlanHandler(db, new FakeKeycloakAdminService(), CreateConfiguration());

        var result = await handler.Handle(new UpdateSubscriptionPlanCommand { Id = entity.Id, Plan = "team", BillingCycle = "Monthly" });

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task UpdateSubscriptionPlanHandler_InvalidPlan_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var entity = CreateTestSubscription(db);
        var handler = new UpdateSubscriptionPlanHandler(db, new FakeKeycloakAdminService(), CreateConfiguration());

        var result = await handler.Handle(new UpdateSubscriptionPlanCommand { Id = entity.Id, Plan = "enterprise", BillingCycle = "Monthly" });

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task UpdateSubscriptionPlanHandler_InvalidBillingCycle_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var entity = CreateTestSubscription(db);
        var handler = new UpdateSubscriptionPlanHandler(db, new FakeKeycloakAdminService(), CreateConfiguration());

        var result = await handler.Handle(new UpdateSubscriptionPlanCommand { Id = entity.Id, Plan = "starter", BillingCycle = "Weekly" });

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task UpdateSubscriptionPlanHandler_PaidWithNullBillingCycle_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var entity = CreateTestSubscription(db);
        var handler = new UpdateSubscriptionPlanHandler(db, new FakeKeycloakAdminService(), CreateConfiguration());

        var result = await handler.Handle(new UpdateSubscriptionPlanCommand { Id = entity.Id, Plan = "starter", BillingCycle = null });

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task UpdateSubscriptionPlanHandler_NonExistent_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new UpdateSubscriptionPlanHandler(db, new FakeKeycloakAdminService(), CreateConfiguration());

        var result = await handler.Handle(new UpdateSubscriptionPlanCommand { Id = Guid.NewGuid(), Plan = "starter", BillingCycle = "Monthly" });

        Assert.True(result.IsT1);
    }

    // --- AdminSubscriptionMapper ---

    [Fact]
    public void DeriveStatus_Pending_ReturnsPending()
    {
        var s = new Subscription { CreatedAtUtc = DateTimeOffset.UtcNow };
        Assert.Equal("pending", AdminSubscriptionMapper.DeriveStatus(s));
    }

    [Fact]
    public void DeriveStatus_Trial_ReturnsTrial()
    {
        var s = new Subscription { CreatedAtUtc = DateTimeOffset.UtcNow, TrialEndsAtUtc = DateTimeOffset.UtcNow.AddDays(30) };
        Assert.Equal("trial", AdminSubscriptionMapper.DeriveStatus(s));
    }

    [Fact]
    public void DeriveStatus_Provisioned_ReturnsProvisioned()
    {
        var s = new Subscription { CreatedAtUtc = DateTimeOffset.UtcNow, ProvisionedAtUtc = DateTimeOffset.UtcNow };
        Assert.Equal("provisioned", AdminSubscriptionMapper.DeriveStatus(s));
    }

    [Fact]
    public void DeriveStatus_Cancelled_ReturnsCancelled()
    {
        var s = new Subscription { CreatedAtUtc = DateTimeOffset.UtcNow, CancelledAtUtc = DateTimeOffset.UtcNow };
        Assert.Equal("cancelled", AdminSubscriptionMapper.DeriveStatus(s));
    }

    [Fact]
    public void ToDetailResponse_WithNameMap_ResolvesNames()
    {
        var userId = Guid.NewGuid();
        var s = new Subscription
        {
            Id = Guid.NewGuid(),
            SalonName = "Test",
            OwnerFirstName = "Jan",
            OwnerLastName = "Jansen",
            Email = "jan@test.nl",
            Plan = SubscriptionPlan.Starter,
            BillingCycle = BillingCycle.Monthly,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = userId,
        };
        var nameMap = new Dictionary<Guid, string> { [userId] = "Jan Admin" };

        var response = AdminSubscriptionMapper.ToDetailResponse(s, nameMap);

        Assert.Equal("Jan Admin", response.CreatedByName);
    }

    [Fact]
    public void ToDetailResponse_WithoutNameMap_FallsBackToGuidString()
    {
        var userId = Guid.NewGuid();
        var s = new Subscription
        {
            Id = Guid.NewGuid(),
            SalonName = "Test",
            OwnerFirstName = "Jan",
            OwnerLastName = "Jansen",
            Email = "jan@test.nl",
            Plan = SubscriptionPlan.Starter,
            BillingCycle = BillingCycle.Monthly,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = userId,
        };

        var response = AdminSubscriptionMapper.ToDetailResponse(s);

        Assert.Equal(userId.ToString(), response.CreatedByName);
    }

    [Fact]
    public void ToDetailResponse_NullUserId_ReturnsNullName()
    {
        var s = new Subscription
        {
            Id = Guid.NewGuid(),
            SalonName = "Test",
            OwnerFirstName = "Jan",
            OwnerLastName = "Jansen",
            Email = "jan@test.nl",
            Plan = SubscriptionPlan.Starter,
            BillingCycle = BillingCycle.Monthly,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        var response = AdminSubscriptionMapper.ToDetailResponse(s);

        Assert.Null(response.CreatedByName);
        Assert.Null(response.ProvisionedByName);
        Assert.Null(response.CancelledByName);
    }

    // --- FakeKeycloakAdminService ---

    private sealed class FakeKeycloakAdminService : IKeycloakAdminService
    {
        private readonly Dictionary<Guid, string> _displayNames = [];

        internal void SetDisplayName(Guid userId, string displayName) =>
            _displayNames[userId] = displayName;

        public Task<string?> GetUserDisplayNameAsync(string realmName, string userId, CancellationToken ct = default) =>
            Task.FromResult(Guid.TryParse(userId, out var id) && _displayNames.TryGetValue(id, out var name) ? name : (string?)null);

        public Task<string> CreateRealmAsync(Guid tenantId, string adminEmail, CancellationToken ct = default) =>
            Task.FromResult(tenantId.ToString());

        public Task<string> CreateUserAsync(Guid tenantId, string email, string firstName, string lastName, string role, CancellationToken ct = default) =>
            Task.FromResult(Guid.NewGuid().ToString());

        public Task UpdateUserAsync(Guid tenantId, string keycloakUserId, string email, string firstName, string lastName, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task DisableUserAsync(Guid tenantId, string keycloakUserId, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task EnableUserAsync(Guid tenantId, string keycloakUserId, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task DeleteUserAsync(Guid tenantId, string keycloakUserId, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task AssignRealmRoleAsync(Guid tenantId, string keycloakUserId, string roleName, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task SetPasswordAsync(Guid tenantId, string keycloakUserId, string password, bool temporary = false, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task SendActionsEmailAsync(Guid tenantId, string keycloakUserId, string[] actions, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task DeleteRealmAsync(Guid tenantId, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
