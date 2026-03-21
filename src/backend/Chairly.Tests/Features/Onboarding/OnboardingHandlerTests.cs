using System.ComponentModel.DataAnnotations;
using Chairly.Api.Features.Onboarding.CreateSubscription;
using Chairly.Api.Features.Onboarding.GetSubscriptionPlans;
using Chairly.Domain.Entities;
using Chairly.Domain.Enums;
using Chairly.Domain.Events;
using Chairly.Infrastructure.Messaging;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Chairly.Tests.Features.Onboarding;

public class OnboardingHandlerTests
{
    private static WebsiteDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WebsiteDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new WebsiteDbContext(options);
    }

    // --- CreateSubscription: Trial happy path ---

    [Fact]
    public async Task CreateSubscriptionHandler_Trial_SetsTrialEndsAtUtcAndStarterPlan()
    {
        await using var db = CreateDbContext();
        var eventPublisher = new SpyOnboardingEventPublisher();
        var handler = new CreateSubscriptionHandler(db, eventPublisher, NullLogger<CreateSubscriptionHandler>.Instance);
        var command = new CreateSubscriptionCommand
        {
            SalonName = "Salon Mooi",
            OwnerFirstName = "Jan",
            OwnerLastName = "de Vries",
            Email = "jan@salonmooi.nl",
            Plan = "starter",
            IsTrial = true,
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("starter", response.Plan);
        Assert.Null(response.BillingCycle);
        Assert.True(response.IsTrial);
        Assert.NotNull(response.TrialEndsAtUtc);
        Assert.True(response.TrialEndsAtUtc > DateTimeOffset.UtcNow.AddDays(29));
        Assert.Equal(1, await db.Subscriptions.CountAsync());
    }

    // --- CreateSubscription: Paid happy path ---

    [Fact]
    public async Task CreateSubscriptionHandler_Paid_SetsNullTrialEndsAtUtcAndBillingCycle()
    {
        await using var db = CreateDbContext();
        var eventPublisher = new SpyOnboardingEventPublisher();
        var handler = new CreateSubscriptionHandler(db, eventPublisher, NullLogger<CreateSubscriptionHandler>.Instance);
        var command = new CreateSubscriptionCommand
        {
            SalonName = "Salon Mooi",
            OwnerFirstName = "Jan",
            OwnerLastName = "de Vries",
            Email = "jan@salonmooi.nl",
            Plan = "team",
            BillingCycle = "Monthly",
            IsTrial = false,
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Equal("team", response.Plan);
        Assert.Equal("Monthly", response.BillingCycle);
        Assert.False(response.IsTrial);
        Assert.Null(response.TrialEndsAtUtc);
    }

    [Fact]
    public async Task CreateSubscriptionHandler_PaidAnnual_SetsAnnualBillingCycle()
    {
        await using var db = CreateDbContext();
        var eventPublisher = new SpyOnboardingEventPublisher();
        var handler = new CreateSubscriptionHandler(db, eventPublisher, NullLogger<CreateSubscriptionHandler>.Instance);
        var command = new CreateSubscriptionCommand
        {
            SalonName = "Salon Mooi",
            OwnerFirstName = "Jan",
            OwnerLastName = "de Vries",
            Email = "jan@salonmooi.nl",
            Plan = "salon",
            BillingCycle = "Annual",
            IsTrial = false,
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Equal("salon", response.Plan);
        Assert.Equal("Annual", response.BillingCycle);
        Assert.False(response.IsTrial);
    }

    // --- CreateSubscription: Validation failures ---

    [Fact]
    public async Task CreateSubscriptionHandler_TrialWithTeamPlan_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var eventPublisher = new SpyOnboardingEventPublisher();
        var handler = new CreateSubscriptionHandler(db, eventPublisher, NullLogger<CreateSubscriptionHandler>.Instance);
        var command = new CreateSubscriptionCommand
        {
            SalonName = "Salon",
            OwnerFirstName = "Jan",
            OwnerLastName = "de Vries",
            Email = "jan@salon.nl",
            Plan = "team",
            IsTrial = true,
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task CreateSubscriptionHandler_TrialWithBillingCycle_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var eventPublisher = new SpyOnboardingEventPublisher();
        var handler = new CreateSubscriptionHandler(db, eventPublisher, NullLogger<CreateSubscriptionHandler>.Instance);
        var command = new CreateSubscriptionCommand
        {
            SalonName = "Salon",
            OwnerFirstName = "Jan",
            OwnerLastName = "de Vries",
            Email = "jan@salon.nl",
            Plan = "starter",
            BillingCycle = "Monthly",
            IsTrial = true,
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task CreateSubscriptionHandler_PaidWithoutBillingCycle_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var eventPublisher = new SpyOnboardingEventPublisher();
        var handler = new CreateSubscriptionHandler(db, eventPublisher, NullLogger<CreateSubscriptionHandler>.Instance);
        var command = new CreateSubscriptionCommand
        {
            SalonName = "Salon",
            OwnerFirstName = "Jan",
            OwnerLastName = "de Vries",
            Email = "jan@salon.nl",
            Plan = "team",
            IsTrial = false,
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task CreateSubscriptionHandler_InvalidPlan_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var eventPublisher = new SpyOnboardingEventPublisher();
        var handler = new CreateSubscriptionHandler(db, eventPublisher, NullLogger<CreateSubscriptionHandler>.Instance);
        var command = new CreateSubscriptionCommand
        {
            SalonName = "Salon",
            OwnerFirstName = "Jan",
            OwnerLastName = "de Vries",
            Email = "jan@salon.nl",
            Plan = "enterprise",
            BillingCycle = "Monthly",
            IsTrial = false,
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task CreateSubscriptionHandler_InvalidBillingCycle_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var eventPublisher = new SpyOnboardingEventPublisher();
        var handler = new CreateSubscriptionHandler(db, eventPublisher, NullLogger<CreateSubscriptionHandler>.Instance);
        var command = new CreateSubscriptionCommand
        {
            SalonName = "Salon",
            OwnerFirstName = "Jan",
            OwnerLastName = "de Vries",
            Email = "jan@salon.nl",
            Plan = "starter",
            BillingCycle = "Weekly",
            IsTrial = false,
        };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
    }

    // --- CreateSubscription: Data Annotations validation ---

    [Fact]
    public void CreateSubscriptionCommand_MissingRequiredFields_FailsValidation()
    {
        var command = new CreateSubscriptionCommand
        {
            SalonName = string.Empty,
            OwnerFirstName = string.Empty,
            OwnerLastName = string.Empty,
            Email = string.Empty,
            Plan = string.Empty,
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubscriptionCommand.SalonName), StringComparer.Ordinal));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubscriptionCommand.OwnerFirstName), StringComparer.Ordinal));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubscriptionCommand.OwnerLastName), StringComparer.Ordinal));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSubscriptionCommand.Email), StringComparer.Ordinal));
    }

    // --- CreateSubscription: Event publishing ---

    [Fact]
    public async Task CreateSubscriptionHandler_HappyPath_PublishesDomainEvent()
    {
        await using var db = CreateDbContext();
        var eventPublisher = new SpyOnboardingEventPublisher();
        var handler = new CreateSubscriptionHandler(db, eventPublisher, NullLogger<CreateSubscriptionHandler>.Instance);
        var command = new CreateSubscriptionCommand
        {
            SalonName = "Salon Mooi",
            OwnerFirstName = "Jan",
            OwnerLastName = "de Vries",
            Email = "jan@salonmooi.nl",
            Plan = "starter",
            BillingCycle = "Monthly",
            IsTrial = false,
        };

        await handler.Handle(command);

        Assert.Single(eventPublisher.SubscriptionCreatedEvents);
        Assert.Equal("Salon Mooi", eventPublisher.SubscriptionCreatedEvents[0].SalonName);
        Assert.Equal("jan@salonmooi.nl", eventPublisher.SubscriptionCreatedEvents[0].Email);
    }

    // --- CreateSubscription: Response plan is lowercase ---

    [Fact]
    public async Task CreateSubscriptionHandler_ResponsePlan_IsLowercaseSlug()
    {
        await using var db = CreateDbContext();
        var eventPublisher = new SpyOnboardingEventPublisher();
        var handler = new CreateSubscriptionHandler(db, eventPublisher, NullLogger<CreateSubscriptionHandler>.Instance);
        var command = new CreateSubscriptionCommand
        {
            SalonName = "Salon",
            OwnerFirstName = "Jan",
            OwnerLastName = "de Vries",
            Email = "jan@salon.nl",
            Plan = "Team",
            BillingCycle = "Annual",
            IsTrial = false,
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Equal("team", response.Plan);
    }

    // --- Subscription entity tests ---

    [Fact]
    public async Task Subscription_CanBePersisted_AndRetrieved()
    {
        await using var db = CreateDbContext();
        var entity = new Subscription
        {
            Id = Guid.NewGuid(),
            SalonName = "Test Salon",
            OwnerFirstName = "Test",
            OwnerLastName = "Owner",
            Email = "test@salon.nl",
            Plan = SubscriptionPlan.Team,
            BillingCycle = Domain.Enums.BillingCycle.Monthly,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        db.Subscriptions.Add(entity);
        await db.SaveChangesAsync();

        var retrieved = await db.Subscriptions.FirstOrDefaultAsync(s => s.Id == entity.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Test Salon", retrieved.SalonName);
    }

    [Fact]
    public void Subscription_Trial_IsTrialReturnsTrue()
    {
        var entity = new Subscription
        {
            TrialEndsAtUtc = DateTimeOffset.UtcNow.AddDays(30),
        };

        Assert.True(entity.IsTrial);
    }

    [Fact]
    public void Subscription_Paid_IsTrialReturnsFalse()
    {
        var entity = new Subscription
        {
            TrialEndsAtUtc = null,
            BillingCycle = Domain.Enums.BillingCycle.Annual,
        };

        Assert.False(entity.IsTrial);
    }

    // --- GetSubscriptionPlans tests ---

    [Fact]
    public async Task GetSubscriptionPlansHandler_Returns3Plans()
    {
        var handler = new GetSubscriptionPlansHandler();

        var result = await handler.Handle(new GetSubscriptionPlansQuery());

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetSubscriptionPlansHandler_PlansAreInOrder()
    {
        var handler = new GetSubscriptionPlansHandler();

        var result = await handler.Handle(new GetSubscriptionPlansQuery());

        Assert.Equal("starter", result[0].Slug);
        Assert.Equal("team", result[1].Slug);
        Assert.Equal("salon", result[2].Slug);
    }

    [Fact]
    public async Task GetSubscriptionPlansHandler_PricesMatchExpected()
    {
        var handler = new GetSubscriptionPlansHandler();

        var result = await handler.Handle(new GetSubscriptionPlansQuery());

        Assert.Equal(14.99m, result[0].MonthlyPrice);
        Assert.Equal(13.49m, result[0].AnnualPricePerMonth);
        Assert.Equal(59.99m, result[1].MonthlyPrice);
        Assert.Equal(53.99m, result[1].AnnualPricePerMonth);
        Assert.Equal(149.00m, result[2].MonthlyPrice);
        Assert.Equal(134.10m, result[2].AnnualPricePerMonth);
    }

    [Fact]
    public async Task GetSubscriptionPlansHandler_MaxStaffValuesAreCorrect()
    {
        var handler = new GetSubscriptionPlansHandler();

        var result = await handler.Handle(new GetSubscriptionPlansQuery());

        Assert.Equal(1, result[0].MaxStaff);
        Assert.Equal(5, result[1].MaxStaff);
        Assert.Equal(15, result[2].MaxStaff);
    }

    // --- Spy event publisher for testing ---

    private sealed class SpyOnboardingEventPublisher : IOnboardingEventPublisher
    {
        public List<SubscriptionCreatedEvent> SubscriptionCreatedEvents { get; } = [];

        public Task PublishSubscriptionCreatedAsync(SubscriptionCreatedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            SubscriptionCreatedEvents.Add(domainEvent);
            return Task.CompletedTask;
        }
    }
}
