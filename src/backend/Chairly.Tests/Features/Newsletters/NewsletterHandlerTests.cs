using Chairly.Api.Features.Newsletters;
using Chairly.Api.Features.Newsletters.CancelNewsletterCampaign;
using Chairly.Api.Features.Newsletters.CreateNewsletterCampaign;
using Chairly.Api.Features.Newsletters.DeleteNewsletterCampaign;
using Chairly.Api.Features.Newsletters.GetNewsletterCampaignDetail;
using Chairly.Api.Features.Newsletters.GetNewsletterCampaignsList;
using Chairly.Api.Features.Newsletters.Infrastructure;
using Chairly.Api.Features.Newsletters.PreviewNewsletter;
using Chairly.Api.Features.Newsletters.ScheduleNewsletterCampaign;
using Chairly.Api.Features.Newsletters.SendNewsletterCampaign;
using Chairly.Api.Features.Newsletters.UpdateNewsletterCampaign;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Chairly.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Chairly.Tests.Features.Newsletters;

public class NewsletterHandlerTests
{
    private static ChairlyDbContext CreateDbContext() => DbContextFactory.Create();

    private static Client CreateSubscribedClient(ChairlyDbContext db, string email = "client@example.com")
    {
        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            FirstName = "Test",
            LastName = "Client",
            Email = email,
            IsSubscribedToNewsletter = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Clients.Add(client);
        db.SaveChanges();
        return client;
    }

    private static NewsletterCampaign CreateDraft(ChairlyDbContext db, string subject = "Test Subject", string body = "<p>Hallo</p>")
    {
        var campaign = new NewsletterCampaign
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantContext.DefaultTenantId,
            Subject = subject,
            BodyHtml = body,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = TestTenantContext.DefaultUserId,
        };
        db.NewsletterCampaigns.Add(campaign);
        db.SaveChanges();
        return campaign;
    }

    [Fact]
    public void Sanitizer_StripsScriptTagsAndHandlers()
    {
        var sanitizer = new NewsletterHtmlSanitizer();
        var input = "<p onclick=\"alert(1)\">Hi</p><script>alert('x')</script>";
        var result = sanitizer.Sanitize(input);
        Assert.DoesNotContain("script", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<p", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Sanitizer_EmptyInputReturnsEmpty()
    {
        var sanitizer = new NewsletterHtmlSanitizer();
        Assert.Equal(string.Empty, sanitizer.Sanitize(string.Empty));
        Assert.Equal(string.Empty, sanitizer.Sanitize("   "));
    }

    [Fact]
    public async Task CreateHandler_PersistsSanitisedHtml()
    {
        await using var db = CreateDbContext();
        var handler = new CreateNewsletterCampaignHandler(db, new NewsletterHtmlSanitizer(), new TestTenantContext());
        var command = new CreateNewsletterCampaignCommand
        {
            Subject = "Lente-actie",
            BodyHtml = "<p>Hallo</p><script>alert(1)</script>",
        };

        var result = await handler.Handle(command);

        var response = result.AsT0;
        Assert.Equal("Lente-actie", response.Subject);
        Assert.DoesNotContain("script", response.BodyHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, await db.NewsletterCampaigns.CountAsync());
    }

    [Fact]
    public async Task CreateHandler_EmptyAfterSanitisation_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var handler = new CreateNewsletterCampaignHandler(db, new NewsletterHtmlSanitizer(), new TestTenantContext());
        var command = new CreateNewsletterCampaignCommand { Subject = "X", BodyHtml = "<script>x</script>" };

        var result = await handler.Handle(command);

        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task UpdateHandler_DraftUpdates()
    {
        await using var db = CreateDbContext();
        var existing = CreateDraft(db);
        var handler = new UpdateNewsletterCampaignHandler(db, new NewsletterHtmlSanitizer(), new TestTenantContext());

        var result = await handler.Handle(new UpdateNewsletterCampaignCommand
        {
            Id = existing.Id,
            Subject = "Nieuw onderwerp",
            BodyHtml = "<p>Update</p>",
        });

        var response = result.AsT0;
        Assert.Equal("Nieuw onderwerp", response.Subject);
    }

    [Fact]
    public async Task UpdateHandler_SentCampaign_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        var existing = CreateDraft(db);
        existing.SentAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new UpdateNewsletterCampaignHandler(db, new NewsletterHtmlSanitizer(), new TestTenantContext());

        var result = await handler.Handle(new UpdateNewsletterCampaignCommand
        {
            Id = existing.Id,
            Subject = "x",
            BodyHtml = "<p>x</p>",
        });

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task UpdateHandler_NotFound_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new UpdateNewsletterCampaignHandler(db, new NewsletterHtmlSanitizer(), new TestTenantContext());
        var result = await handler.Handle(new UpdateNewsletterCampaignCommand
        {
            Id = Guid.NewGuid(),
            Subject = "x",
            BodyHtml = "<p>x</p>",
        });
        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task ListHandler_ReturnsOrderedDescByCreated()
    {
        await using var db = CreateDbContext();
        var older = CreateDraft(db, subject: "Oud");
        older.CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1);
        var newer = CreateDraft(db, subject: "Nieuw");
        await db.SaveChangesAsync();

        var handler = new GetNewsletterCampaignsListHandler(db, new TestTenantContext());
        var list = await handler.Handle(new GetNewsletterCampaignsListQuery());

        Assert.Equal(2, list.Count);
        Assert.Equal(newer.Id, list[0].Id);
    }

    [Fact]
    public async Task DetailHandler_ReturnsCounts()
    {
        await using var db = CreateDbContext();
        CreateSubscribedClient(db);
        var campaign = CreateDraft(db);
        var handler = new GetNewsletterCampaignDetailHandler(db, new TestTenantContext());

        var result = await handler.Handle(new GetNewsletterCampaignDetailQuery(campaign.Id));

        var response = result.AsT0;
        Assert.Equal(0, response.TotalRecipients);
        Assert.Equal(1, response.EligibleSubscribers);
        Assert.Equal(NewsletterStatus.Draft, response.Status);
    }

    [Fact]
    public async Task DeleteHandler_DraftDeletes()
    {
        await using var db = CreateDbContext();
        var draft = CreateDraft(db);
        var handler = new DeleteNewsletterCampaignHandler(db, new TestTenantContext());

        var result = await handler.Handle(new DeleteNewsletterCampaignCommand(draft.Id));

        Assert.True(result.IsT0);
        Assert.Equal(0, await db.NewsletterCampaigns.CountAsync());
    }

    [Fact]
    public async Task DeleteHandler_SendingReturnsConflict()
    {
        await using var db = CreateDbContext();
        var draft = CreateDraft(db);
        draft.QueuedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new DeleteNewsletterCampaignHandler(db, new TestTenantContext());

        var result = await handler.Handle(new DeleteNewsletterCampaignCommand(draft.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task DeleteHandler_SentReturnsConflict()
    {
        await using var db = CreateDbContext();
        var draft = CreateDraft(db);
        draft.SentAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new DeleteNewsletterCampaignHandler(db, new TestTenantContext());

        var result = await handler.Handle(new DeleteNewsletterCampaignCommand(draft.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task DeleteHandler_ScheduledCampaignDeletes()
    {
        await using var db = CreateDbContext();
        var draft = CreateDraft(db);
        draft.ScheduledAtUtc = DateTimeOffset.UtcNow.AddHours(1);
        await db.SaveChangesAsync();
        var handler = new DeleteNewsletterCampaignHandler(db, new TestTenantContext());

        var result = await handler.Handle(new DeleteNewsletterCampaignCommand(draft.Id));

        Assert.True(result.IsT0);
        Assert.Equal(0, await db.NewsletterCampaigns.CountAsync());
    }

    [Fact]
    public async Task DeleteHandler_CancelledCampaignReturnsConflict()
    {
        await using var db = CreateDbContext();
        var draft = CreateDraft(db);
        draft.CancelledAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new DeleteNewsletterCampaignHandler(db, new TestTenantContext());

        var result = await handler.Handle(new DeleteNewsletterCampaignCommand(draft.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task DeleteHandler_UnknownId_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new DeleteNewsletterCampaignHandler(db, new TestTenantContext());

        var result = await handler.Handle(new DeleteNewsletterCampaignCommand(Guid.NewGuid()));

        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task CancelHandler_UnknownId_ReturnsNotFound()
    {
        await using var db = CreateDbContext();
        var handler = new CancelNewsletterCampaignHandler(db, new TestTenantContext());

        var result = await handler.Handle(new CancelNewsletterCampaignCommand(Guid.NewGuid()));

        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task SendHandler_CancelledCampaign_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        CreateSubscribedClient(db);
        var draft = CreateDraft(db);
        draft.CancelledAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new SendNewsletterCampaignHandler(db, new NullNewsletterEventPublisher(), new TestTenantContext(), NullLogger<SendNewsletterCampaignHandler>.Instance);

        var result = await handler.Handle(new SendNewsletterCampaignCommand(draft.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task ScheduleHandler_FuturePersists()
    {
        await using var db = CreateDbContext();
        var draft = CreateDraft(db);
        var handler = new ScheduleNewsletterCampaignHandler(db, new TestTenantContext());

        var result = await handler.Handle(new ScheduleNewsletterCampaignCommand
        {
            Id = draft.Id,
            ScheduledAtUtc = DateTimeOffset.UtcNow.AddHours(1),
        });

        Assert.True(result.IsT0);
        var fresh = await db.NewsletterCampaigns.FindAsync(draft.Id);
        Assert.NotNull(fresh!.ScheduledAtUtc);
    }

    [Fact]
    public async Task ScheduleHandler_PastReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var draft = CreateDraft(db);
        var handler = new ScheduleNewsletterCampaignHandler(db, new TestTenantContext());

        var result = await handler.Handle(new ScheduleNewsletterCampaignCommand
        {
            Id = draft.Id,
            ScheduledAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
        });

        Assert.True(result.IsT3);
    }

    [Fact]
    public async Task CancelHandler_DraftCancels()
    {
        await using var db = CreateDbContext();
        var draft = CreateDraft(db);
        var handler = new CancelNewsletterCampaignHandler(db, new TestTenantContext());

        var result = await handler.Handle(new CancelNewsletterCampaignCommand(draft.Id));

        Assert.True(result.IsT0);
        var fresh = await db.NewsletterCampaigns.FindAsync(draft.Id);
        Assert.NotNull(fresh!.CancelledAtUtc);
    }

    [Fact]
    public async Task CancelHandler_SentReturnsConflict()
    {
        await using var db = CreateDbContext();
        var draft = CreateDraft(db);
        draft.SentAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var handler = new CancelNewsletterCampaignHandler(db, new TestTenantContext());

        var result = await handler.Handle(new CancelNewsletterCampaignCommand(draft.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task SendHandler_MaterialisesRecipientsAndPublishes()
    {
        await using var db = CreateDbContext();
        CreateSubscribedClient(db, "a@example.com");
        CreateSubscribedClient(db, "b@example.com");
        var draft = CreateDraft(db);
        var publisher = new RecordingNewsletterEventPublisher();
        var handler = new SendNewsletterCampaignHandler(db, publisher, new TestTenantContext(), NullLogger<SendNewsletterCampaignHandler>.Instance);

        var result = await handler.Handle(new SendNewsletterCampaignCommand(draft.Id));

        Assert.True(result.IsT0);
        Assert.Equal(2, await db.NewsletterDeliveries.CountAsync());
        Assert.Single(publisher.CampaignQueuedEvents);
        var tokens = await db.NewsletterDeliveries.Select(d => d.UnsubscribeToken).ToListAsync();
        Assert.Equal(tokens.Count, tokens.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task SendHandler_NoRecipients_ReturnsUnprocessable()
    {
        await using var db = CreateDbContext();
        var draft = CreateDraft(db);
        var publisher = new RecordingNewsletterEventPublisher();
        var handler = new SendNewsletterCampaignHandler(db, publisher, new TestTenantContext(), NullLogger<SendNewsletterCampaignHandler>.Instance);

        var result = await handler.Handle(new SendNewsletterCampaignCommand(draft.Id));

        Assert.True(result.IsT3);
        Assert.Empty(publisher.CampaignQueuedEvents);
    }

    [Fact]
    public async Task SendHandler_AlreadySent_ReturnsConflict()
    {
        await using var db = CreateDbContext();
        CreateSubscribedClient(db);
        var draft = CreateDraft(db);
        draft.SentAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        var publisher = new RecordingNewsletterEventPublisher();
        var handler = new SendNewsletterCampaignHandler(db, publisher, new TestTenantContext(), NullLogger<SendNewsletterCampaignHandler>.Instance);

        var result = await handler.Handle(new SendNewsletterCampaignCommand(draft.Id));

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task PreviewHandler_StripsScripts()
    {
        await using var db = CreateDbContext();
        var handler = new PreviewNewsletterHandler(db, new NewsletterHtmlSanitizer(), new TestTenantContext());

        var result = await handler.Handle(new PreviewNewsletterCommand
        {
            Subject = "Hi",
            BodyHtml = "<p>Hallo</p><script>x</script>",
        });

        Assert.DoesNotContain("script", result.HtmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Uitschrijven", result.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public void StatusDerivation_MatchesLifecycle()
    {
        var draft = new NewsletterCampaign();
        Assert.Equal(NewsletterStatus.Draft, NewsletterStatus.Derive(draft));

        draft.ScheduledAtUtc = DateTimeOffset.UtcNow;
        Assert.Equal(NewsletterStatus.Scheduled, NewsletterStatus.Derive(draft));

        draft.QueuedAtUtc = DateTimeOffset.UtcNow;
        Assert.Equal(NewsletterStatus.Sending, NewsletterStatus.Derive(draft));

        draft.SentAtUtc = DateTimeOffset.UtcNow;
        Assert.Equal(NewsletterStatus.Sent, NewsletterStatus.Derive(draft));

        var cancelled = new NewsletterCampaign { CancelledAtUtc = DateTimeOffset.UtcNow };
        Assert.Equal(NewsletterStatus.Cancelled, NewsletterStatus.Derive(cancelled));
    }
}
