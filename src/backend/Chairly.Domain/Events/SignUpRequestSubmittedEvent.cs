namespace Chairly.Domain.Events;

public record SignUpRequestSubmittedEvent(
    Guid SignUpRequestId,
    string SalonName,
    string OwnerFirstName,
    string OwnerLastName,
    string Email,
    string? PhoneNumber);
