using Chairly.Domain.Events;

namespace Chairly.Infrastructure.Messaging;

public interface IOnboardingEventPublisher
{
    Task PublishDemoRequestSubmittedAsync(DemoRequestSubmittedEvent domainEvent, CancellationToken cancellationToken = default);
    Task PublishSignUpRequestSubmittedAsync(SignUpRequestSubmittedEvent domainEvent, CancellationToken cancellationToken = default);
}
