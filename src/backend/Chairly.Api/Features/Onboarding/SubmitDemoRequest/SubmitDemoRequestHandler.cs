using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Domain.Entities;
using Chairly.Domain.Events;
using Chairly.Infrastructure.Messaging;
using Chairly.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using OneOf;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Onboarding.SubmitDemoRequest;

internal sealed partial class SubmitDemoRequestHandler(
    WebsiteDbContext db,
    IOnboardingEventPublisher eventPublisher,
    ILogger<SubmitDemoRequestHandler> logger) : IRequestHandler<SubmitDemoRequestCommand, OneOf<SubmitDemoRequestResponse, Unprocessable>>
{
    public async Task<OneOf<SubmitDemoRequestResponse, Unprocessable>> Handle(SubmitDemoRequestCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var entity = new DemoRequest
        {
            Id = Guid.NewGuid(),
            ContactName = command.ContactName,
            SalonName = command.SalonName,
            Email = command.Email,
            PhoneNumber = command.PhoneNumber,
            Message = command.Message,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = null,
        };

        db.DemoRequests.Add(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await eventPublisher.PublishDemoRequestSubmittedAsync(
                new DemoRequestSubmittedEvent(
                    entity.Id,
                    entity.ContactName,
                    entity.SalonName,
                    entity.Email,
                    entity.PhoneNumber,
                    entity.Message),
                cancellationToken).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Best-effort event publishing; demo request is already persisted
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogEventPublishFailed(logger, entity.Id, ex);
        }

        return ToResponse(entity);
    }

    private static SubmitDemoRequestResponse ToResponse(DemoRequest entity) =>
        new(entity.Id, entity.ContactName, entity.SalonName, entity.Email, entity.CreatedAtUtc);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to publish event for demo request {DemoRequestId}; notification may be delayed")]
    private static partial void LogEventPublishFailed(ILogger logger, Guid demoRequestId, Exception exception);
}
#pragma warning restore CA1812
