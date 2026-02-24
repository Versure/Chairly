# ADR-004: RabbitMQ for Async Events

## Status

Accepted

## Context

Certain operations (booking confirmations, reminders, notifications) should be handled asynchronously to avoid blocking the request pipeline. We need a message broker for event-driven communication.

## Decision

We use **RabbitMQ** as the message broker for asynchronous events.

- RabbitMQ runs as a container managed by .NET Aspire (see ADR-002)
- Domain events are published to RabbitMQ after successful command execution
- Consumer services process events independently (e.g. sending emails, SMS)
- We use MassTransit or a lightweight wrapper around the RabbitMQ .NET client (to be decided during implementation)

**Example events:**
- `BookingCreated` → triggers confirmation notification
- `BookingCancelled` → triggers cancellation notification
- `BookingReminder` → scheduled reminder before booking start time

## Consequences

- **Positive:** Decouples command execution from side effects (notifications, integrations).
- **Positive:** Enables retry logic for failed deliveries without affecting the main request.
- **Positive:** RabbitMQ is battle-tested, open-source, and has excellent .NET support.
- **Negative:** Adds infrastructure complexity — another service to run and monitor.
- **Negative:** Eventual consistency — notifications may be slightly delayed.
