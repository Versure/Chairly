namespace Chairly.Domain.Events;

public record DemoRequestSubmittedEvent(
    Guid DemoRequestId,
    string ContactName,
    string SalonName,
    string Email,
    string? PhoneNumber,
    string? Message);
