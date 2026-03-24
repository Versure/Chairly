# Domain Events (Side Effects)

When a handler needs to trigger side effects (send emails, publish messages, update external systems),
use the domain events pattern instead of calling services directly in the handler.

**Pattern (see `ConfirmBookingHandler` and `IBookingEventPublisher` for reference):**

1. Define a domain event record in `Chairly.Domain/Events/`:
```csharp
namespace Chairly.Domain.Events;

public sealed record {Entity}{Action}Event(Guid Id, /* relevant fields */);
```

2. Define a publisher interface in `Chairly.Infrastructure/Messaging/`:
```csharp
namespace Chairly.Infrastructure.Messaging;

public interface I{Context}EventPublisher
{
    Task Publish{Entity}{Action}Async({Entity}{Action}Event domainEvent, CancellationToken cancellationToken);
}
```

3. Implement the publisher in `Chairly.Api/Features/{Context}/`:
```csharp
// Sends emails, publishes to RabbitMQ, etc.
// Best-effort: catch exceptions, log, and continue (entity is already persisted)
```

4. In the handler, inject the publisher and call it AFTER `SaveChangesAsync`:
```csharp
await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

try
{
    await eventPublisher.Publish{Entity}{Action}Async(
        new {Entity}{Action}Event(entity.Id, ...),
        cancellationToken).ConfigureAwait(false);
}
catch (Exception ex)
{
    // Log and continue — entity is already persisted
    Log.{Action}EventFailed(logger, entity.Id, ex);
}
```

**Rules:**
- Handlers must NEVER call `IEmailSender`, `ISmtpService`, or external APIs directly
- All side effects go through domain event publishers
- Event publishing is best-effort: exceptions are caught and logged, not re-thrown
- Register the publisher in `Program.cs` DI
