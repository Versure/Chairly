# RabbitMQ Topology

This document describes the RabbitMQ exchange/queue topology used in Chairly.

## Overview

Chairly uses **raw RabbitMQ.Client** (no MassTransit) with topic exchanges for event-driven communication between bounded contexts.

## Topology

### Exchange: `chairly.bookings`

| Property | Value |
|----------|-------|
| Type | Topic |
| Durable | Yes |
| Auto-delete | No |

**Routing keys:**

| Key | Event | Published by |
|-----|-------|-------------|
| `booking.created` | New booking created | `BookingEventPublisher` |
| `booking.confirmed` | Booking confirmed by staff | `BookingEventPublisher` |
| `booking.cancelled` | Booking cancelled | `BookingEventPublisher` |

### Queue: `notifications.bookings`

| Property | Value |
|----------|-------|
| Durable | Yes |
| Exclusive | No |
| Auto-delete | No |
| Binding | `chairly.bookings` with pattern `booking.*` |

**Consumer:** `BookingEventConsumer` (BackgroundService)

**Message handling by routing key:**

| Routing key | Action |
|-------------|--------|
| `booking.created` | Creates `BookingReceived` notification + `BookingReminder` (24h before start) |
| `booking.confirmed` | Creates `BookingConfirmation` notification |
| `booking.cancelled` | Creates `BookingCancellation` notification + voids pending reminders |

## Message Format

All messages are JSON-serialized with:
- Content type: `application/json`
- Delivery mode: Persistent (survives broker restart)
- Acknowledgment: Manual ACK (NACK without requeue on failure)

### Event Payloads

**BookingCreatedEvent:**
```json
{
  "bookingId": "guid",
  "tenantId": "guid",
  "clientId": "guid",
  "staffMemberId": "guid",
  "startTime": "ISO 8601",
  "endTime": "ISO 8601",
  "clientName": "string",
  "staffMemberName": "string",
  "serviceSummary": "string"
}
```

**BookingConfirmedEvent / BookingCancelledEvent:**
```json
{
  "bookingId": "guid",
  "tenantId": "guid",
  "clientId": "guid",
  "startTime": "ISO 8601",
  "clientName": "string",
  "staffMemberName": "string",
  "serviceSummary": "string"
}
```

## Key Files

| File | Role |
|------|------|
| `Chairly.Infrastructure/Messaging/BookingEventPublisher.cs` | Publishes events to exchange |
| `Chairly.Api/Features/Notifications/Infrastructure/BookingEventConsumer.cs` | Consumes events, creates notifications |
| `Chairly.Domain/Events/BookingCreatedEvent.cs` | Event payload record |
| `Chairly.Domain/Events/BookingConfirmedEvent.cs` | Event payload record |
| `Chairly.Domain/Events/BookingCancelledEvent.cs` | Event payload record |

## Adding a New Exchange/Consumer

1. Create exchange constant and publisher in `Chairly.Infrastructure/Messaging/`
2. Create consumer as `BackgroundService` in `Chairly.Api/Features/{Context}/Infrastructure/`
3. Both publisher and consumer must declare exchange/queue idempotently
4. Register consumer in `Program.cs` with `builder.Services.AddHostedService<>()`
5. Register publisher interface + implementation in DI
6. Use manual ACK — NACK without requeue on failure
7. Update this document with the new topology
