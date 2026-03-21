using Chairly.Api.Shared.Mediator;
using Chairly.Domain.Entities;
using Chairly.Domain.Events;
using Chairly.Infrastructure.Messaging;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Onboarding.SubmitSignUpRequest;

internal sealed partial class SubmitSignUpRequestHandler(
    WebsiteDbContext db,
    IOnboardingEventPublisher eventPublisher,
    ILogger<SubmitSignUpRequestHandler> logger) : IRequestHandler<SubmitSignUpRequestCommand, SubmitSignUpRequestResponse>
{
    public async Task<SubmitSignUpRequestResponse> Handle(SubmitSignUpRequestCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Silent-success pattern: if a pending sign-up request with the same email exists,
        // return a synthetic response without creating a duplicate or sending a notification.
        var hasPendingRequest = await db.SignUpRequests
            .AnyAsync(
                r => r.Email == command.Email
                     && r.ProvisionedAtUtc == null
                     && r.RejectedAtUtc == null,
                cancellationToken)
            .ConfigureAwait(false);

        if (hasPendingRequest)
        {
            return new SubmitSignUpRequestResponse(
                Guid.NewGuid(),
                command.SalonName,
                command.OwnerFirstName,
                command.OwnerLastName,
                command.Email,
                DateTimeOffset.UtcNow);
        }

        var entity = new SignUpRequest
        {
            Id = Guid.NewGuid(),
            SalonName = command.SalonName,
            OwnerFirstName = command.OwnerFirstName,
            OwnerLastName = command.OwnerLastName,
            Email = command.Email,
            PhoneNumber = command.PhoneNumber,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = null,
        };

        db.SignUpRequests.Add(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await eventPublisher.PublishSignUpRequestSubmittedAsync(
                new SignUpRequestSubmittedEvent(
                    entity.Id,
                    entity.SalonName,
                    entity.OwnerFirstName,
                    entity.OwnerLastName,
                    entity.Email,
                    entity.PhoneNumber),
                cancellationToken).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Best-effort event publishing; sign-up request is already persisted
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogEventPublishFailed(logger, entity.Id, ex);
        }

        return ToResponse(entity);
    }

    private static SubmitSignUpRequestResponse ToResponse(SignUpRequest entity) =>
        new(entity.Id, entity.SalonName, entity.OwnerFirstName, entity.OwnerLastName, entity.Email, entity.CreatedAtUtc);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to publish event for sign-up request {SignUpRequestId}; notification may be delayed")]
    private static partial void LogEventPublishFailed(ILogger logger, Guid signUpRequestId, Exception exception);
}
#pragma warning restore CA1812
