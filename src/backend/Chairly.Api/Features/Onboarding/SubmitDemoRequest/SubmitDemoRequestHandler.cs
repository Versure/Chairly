using Chairly.Api.Features.Notifications.Infrastructure;
using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using Chairly.Domain.Entities;
using Chairly.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using OneOf;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Onboarding.SubmitDemoRequest;

internal sealed class SubmitDemoRequestHandler(
    WebsiteDbContext db,
    IEmailSender emailSender,
    IOptions<OnboardingSettings> onboardingSettings) : IRequestHandler<SubmitDemoRequestCommand, OneOf<SubmitDemoRequestResponse, Unprocessable>>
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

        var adminEmail = onboardingSettings.Value.AdminEmail;
        var htmlBody = $"""
            <h2>Nieuwe demo-aanvraag</h2>
            <p><strong>Contactpersoon:</strong> {entity.ContactName}</p>
            <p><strong>Salonnaam:</strong> {entity.SalonName}</p>
            <p><strong>E-mail:</strong> {entity.Email}</p>
            <p><strong>Telefoon:</strong> {entity.PhoneNumber ?? "-"}</p>
            <p><strong>Bericht:</strong> {entity.Message ?? "-"}</p>
            """;

        await emailSender.SendAsync(
            adminEmail,
            "Chairly Admin",
            $"Nieuwe demo-aanvraag: {entity.SalonName}",
            htmlBody,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ToResponse(entity);
    }

    private static SubmitDemoRequestResponse ToResponse(DemoRequest entity) =>
        new(entity.Id, entity.ContactName, entity.SalonName, entity.Email, entity.CreatedAtUtc);
}
#pragma warning restore CA1812
