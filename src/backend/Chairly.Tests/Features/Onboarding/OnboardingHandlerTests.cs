using System.ComponentModel.DataAnnotations;
using Chairly.Api.Features.Onboarding.SubmitDemoRequest;
using Chairly.Api.Features.Onboarding.SubmitSignUpRequest;
using Chairly.Domain.Entities;
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

    // --- SubmitDemoRequest tests ---

    [Fact]
    public async Task SubmitDemoRequestHandler_HappyPath_CreatesDemoRequest()
    {
        await using var db = CreateDbContext();
        var eventPublisher = new SpyOnboardingEventPublisher();
        var handler = new SubmitDemoRequestHandler(db, eventPublisher, NullLogger<SubmitDemoRequestHandler>.Instance);
        var command = new SubmitDemoRequestCommand
        {
            ContactName = "Jan de Vries",
            SalonName = "Salon Mooi",
            Email = "jan@salonmooi.nl",
            PhoneNumber = "0612345678",
            Message = "Ik wil graag een demo.",
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("Jan de Vries", response.ContactName);
        Assert.Equal("Salon Mooi", response.SalonName);
        Assert.Equal("jan@salonmooi.nl", response.Email);
        Assert.Equal(1, await db.DemoRequests.CountAsync());
    }

    [Fact]
    public async Task SubmitDemoRequestHandler_HappyPath_PublishesDomainEvent()
    {
        await using var db = CreateDbContext();
        var eventPublisher = new SpyOnboardingEventPublisher();
        var handler = new SubmitDemoRequestHandler(db, eventPublisher, NullLogger<SubmitDemoRequestHandler>.Instance);
        var command = new SubmitDemoRequestCommand
        {
            ContactName = "Jan de Vries",
            SalonName = "Salon Mooi",
            Email = "jan@salonmooi.nl",
        };

        await handler.Handle(command);

        Assert.Single(eventPublisher.DemoRequestEvents);
        Assert.Equal("Salon Mooi", eventPublisher.DemoRequestEvents[0].SalonName);
        Assert.Equal("jan@salonmooi.nl", eventPublisher.DemoRequestEvents[0].Email);
    }

    [Fact]
    public void SubmitDemoRequestCommand_MissingRequiredFields_FailsValidation()
    {
        var command = new SubmitDemoRequestCommand
        {
            ContactName = string.Empty,
            SalonName = string.Empty,
            Email = string.Empty,
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SubmitDemoRequestCommand.ContactName), StringComparer.Ordinal));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SubmitDemoRequestCommand.SalonName), StringComparer.Ordinal));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SubmitDemoRequestCommand.Email), StringComparer.Ordinal));
    }

    [Fact]
    public void SubmitDemoRequestCommand_InvalidEmail_FailsValidation()
    {
        var command = new SubmitDemoRequestCommand
        {
            ContactName = "Jan",
            SalonName = "Salon",
            Email = "not-an-email",
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SubmitDemoRequestCommand.Email), StringComparer.Ordinal));
    }

    // --- SubmitSignUpRequest tests ---

    [Fact]
    public async Task SubmitSignUpRequestHandler_HappyPath_CreatesSignUpRequest()
    {
        await using var db = CreateDbContext();
        var eventPublisher = new SpyOnboardingEventPublisher();
        var handler = new SubmitSignUpRequestHandler(db, eventPublisher, NullLogger<SubmitSignUpRequestHandler>.Instance);
        var command = new SubmitSignUpRequestCommand
        {
            SalonName = "Salon Mooi",
            OwnerFirstName = "Jan",
            OwnerLastName = "de Vries",
            Email = "jan@salonmooi.nl",
            PhoneNumber = "0612345678",
        };

        var result = await handler.Handle(command);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Salon Mooi", result.SalonName);
        Assert.Equal("Jan", result.OwnerFirstName);
        Assert.Equal("de Vries", result.OwnerLastName);
        Assert.Equal("jan@salonmooi.nl", result.Email);
        Assert.Equal(1, await db.SignUpRequests.CountAsync());
    }

    [Fact]
    public async Task SubmitSignUpRequestHandler_HappyPath_PublishesDomainEvent()
    {
        await using var db = CreateDbContext();
        var eventPublisher = new SpyOnboardingEventPublisher();
        var handler = new SubmitSignUpRequestHandler(db, eventPublisher, NullLogger<SubmitSignUpRequestHandler>.Instance);
        var command = new SubmitSignUpRequestCommand
        {
            SalonName = "Salon Mooi",
            OwnerFirstName = "Jan",
            OwnerLastName = "de Vries",
            Email = "jan@salonmooi.nl",
        };

        await handler.Handle(command);

        Assert.Single(eventPublisher.SignUpRequestEvents);
        Assert.Equal("Salon Mooi", eventPublisher.SignUpRequestEvents[0].SalonName);
        Assert.Equal("jan@salonmooi.nl", eventPublisher.SignUpRequestEvents[0].Email);
    }

    [Fact]
    public async Task SubmitSignUpRequestHandler_DuplicatePendingEmail_ReturnsSilentSuccess()
    {
        await using var db = CreateDbContext();
        var eventPublisher = new SpyOnboardingEventPublisher();
        var handler = new SubmitSignUpRequestHandler(db, eventPublisher, NullLogger<SubmitSignUpRequestHandler>.Instance);

        // Create an existing pending sign-up request
        db.SignUpRequests.Add(new SignUpRequest
        {
            Id = Guid.NewGuid(),
            SalonName = "Salon Existing",
            OwnerFirstName = "Bestaand",
            OwnerLastName = "Persoon",
            Email = "jan@salonmooi.nl",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var command = new SubmitSignUpRequestCommand
        {
            SalonName = "Salon Mooi",
            OwnerFirstName = "Jan",
            OwnerLastName = "de Vries",
            Email = "jan@salonmooi.nl",
        };

        var result = await handler.Handle(command);

        // Should return a synthetic response (silent success)
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Salon Mooi", result.SalonName);
        // Should NOT create a duplicate record
        Assert.Equal(1, await db.SignUpRequests.CountAsync());
        // Should NOT publish a domain event
        Assert.Empty(eventPublisher.SignUpRequestEvents);
    }

    [Fact]
    public async Task SubmitSignUpRequestHandler_PreviouslyRejected_CreatesNewRequest()
    {
        await using var db = CreateDbContext();
        var eventPublisher = new SpyOnboardingEventPublisher();
        var handler = new SubmitSignUpRequestHandler(db, eventPublisher, NullLogger<SubmitSignUpRequestHandler>.Instance);

        // Create a rejected sign-up request with the same email
        db.SignUpRequests.Add(new SignUpRequest
        {
            Id = Guid.NewGuid(),
            SalonName = "Salon Old",
            OwnerFirstName = "Old",
            OwnerLastName = "Request",
            Email = "jan@salonmooi.nl",
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-7),
            RejectedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
        });
        await db.SaveChangesAsync();

        var command = new SubmitSignUpRequestCommand
        {
            SalonName = "Salon Mooi",
            OwnerFirstName = "Jan",
            OwnerLastName = "de Vries",
            Email = "jan@salonmooi.nl",
        };

        var result = await handler.Handle(command);

        // Should create a new record (previous was rejected, not pending)
        Assert.Equal(2, await db.SignUpRequests.CountAsync());
        Assert.Single(eventPublisher.SignUpRequestEvents);
    }

    [Fact]
    public async Task SubmitSignUpRequestHandler_PreviouslyProvisioned_CreatesNewRequest()
    {
        await using var db = CreateDbContext();
        var eventPublisher = new SpyOnboardingEventPublisher();
        var handler = new SubmitSignUpRequestHandler(db, eventPublisher, NullLogger<SubmitSignUpRequestHandler>.Instance);

        // Create a provisioned sign-up request with the same email
        db.SignUpRequests.Add(new SignUpRequest
        {
            Id = Guid.NewGuid(),
            SalonName = "Salon Old",
            OwnerFirstName = "Old",
            OwnerLastName = "Request",
            Email = "jan@salonmooi.nl",
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-7),
            ProvisionedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
        });
        await db.SaveChangesAsync();

        var command = new SubmitSignUpRequestCommand
        {
            SalonName = "Salon Mooi",
            OwnerFirstName = "Jan",
            OwnerLastName = "de Vries",
            Email = "jan@salonmooi.nl",
        };

        var result = await handler.Handle(command);

        Assert.Equal(2, await db.SignUpRequests.CountAsync());
        Assert.Single(eventPublisher.SignUpRequestEvents);
    }

    [Fact]
    public void SubmitSignUpRequestCommand_MissingRequiredFields_FailsValidation()
    {
        var command = new SubmitSignUpRequestCommand
        {
            SalonName = string.Empty,
            OwnerFirstName = string.Empty,
            OwnerLastName = string.Empty,
            Email = string.Empty,
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SubmitSignUpRequestCommand.SalonName), StringComparer.Ordinal));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SubmitSignUpRequestCommand.OwnerFirstName), StringComparer.Ordinal));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SubmitSignUpRequestCommand.OwnerLastName), StringComparer.Ordinal));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SubmitSignUpRequestCommand.Email), StringComparer.Ordinal));
    }

    [Fact]
    public void SubmitSignUpRequestCommand_InvalidEmail_FailsValidation()
    {
        var command = new SubmitSignUpRequestCommand
        {
            SalonName = "Salon",
            OwnerFirstName = "Jan",
            OwnerLastName = "de Vries",
            Email = "not-an-email",
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(command);

        var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SubmitSignUpRequestCommand.Email), StringComparer.Ordinal));
    }

    // --- Spy event publisher for testing ---

    private sealed class SpyOnboardingEventPublisher : IOnboardingEventPublisher
    {
        public List<DemoRequestSubmittedEvent> DemoRequestEvents { get; } = [];
        public List<SignUpRequestSubmittedEvent> SignUpRequestEvents { get; } = [];

        public Task PublishDemoRequestSubmittedAsync(DemoRequestSubmittedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            DemoRequestEvents.Add(domainEvent);
            return Task.CompletedTask;
        }

        public Task PublishSignUpRequestSubmittedAsync(SignUpRequestSubmittedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            SignUpRequestEvents.Add(domainEvent);
            return Task.CompletedTask;
        }
    }
}
