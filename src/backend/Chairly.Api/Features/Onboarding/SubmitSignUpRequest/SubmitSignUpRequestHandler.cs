using Chairly.Api.Features.Notifications.Infrastructure;
using Chairly.Api.Shared.Mediator;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Onboarding.SubmitSignUpRequest;

internal sealed class SubmitSignUpRequestHandler(
    WebsiteDbContext db,
    IEmailSender emailSender,
    IOptions<OnboardingSettings> onboardingSettings) : IRequestHandler<SubmitSignUpRequestCommand, SubmitSignUpRequestResponse>
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

        var adminEmail = onboardingSettings.Value.AdminEmail;
        var htmlBody = $"""
            <h2>Nieuwe aanmelding</h2>
            <p><strong>Salonnaam:</strong> {entity.SalonName}</p>
            <p><strong>Eigenaar:</strong> {entity.OwnerFirstName} {entity.OwnerLastName}</p>
            <p><strong>E-mail:</strong> {entity.Email}</p>
            <p><strong>Telefoon:</strong> {entity.PhoneNumber ?? "-"}</p>
            """;

        await emailSender.SendAsync(
            adminEmail,
            "Chairly Admin",
            $"Nieuwe aanmelding: {entity.SalonName}",
            htmlBody,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ToResponse(entity);
    }

    private static SubmitSignUpRequestResponse ToResponse(SignUpRequest entity) =>
        new(entity.Id, entity.SalonName, entity.OwnerFirstName, entity.OwnerLastName, entity.Email, entity.CreatedAtUtc);
}
#pragma warning restore CA1812
