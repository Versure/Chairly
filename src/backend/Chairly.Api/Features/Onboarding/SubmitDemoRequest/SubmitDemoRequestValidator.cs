using System.ComponentModel.DataAnnotations;

#pragma warning disable CA1812 // Instantiated via validation pipeline
namespace Chairly.Api.Features.Onboarding.SubmitDemoRequest;

internal sealed class SubmitDemoRequestValidator
{
    public static IEnumerable<ValidationResult> Validate(SubmitDemoRequestCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.ContactName))
        {
            yield return new ValidationResult("ContactName is required.", [nameof(SubmitDemoRequestCommand.ContactName)]);
        }
        else if (command.ContactName.Length > 200)
        {
            yield return new ValidationResult("ContactName must not exceed 200 characters.", [nameof(SubmitDemoRequestCommand.ContactName)]);
        }

        if (string.IsNullOrWhiteSpace(command.SalonName))
        {
            yield return new ValidationResult("SalonName is required.", [nameof(SubmitDemoRequestCommand.SalonName)]);
        }
        else if (command.SalonName.Length > 200)
        {
            yield return new ValidationResult("SalonName must not exceed 200 characters.", [nameof(SubmitDemoRequestCommand.SalonName)]);
        }

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            yield return new ValidationResult("Email is required.", [nameof(SubmitDemoRequestCommand.Email)]);
        }
        else
        {
            if (command.Email.Length > 256)
            {
                yield return new ValidationResult("Email must not exceed 256 characters.", [nameof(SubmitDemoRequestCommand.Email)]);
            }

            if (!new EmailAddressAttribute().IsValid(command.Email))
            {
                yield return new ValidationResult("Email must be a valid email address.", [nameof(SubmitDemoRequestCommand.Email)]);
            }
        }

        if (command.PhoneNumber is not null && command.PhoneNumber.Length > 50)
        {
            yield return new ValidationResult("PhoneNumber must not exceed 50 characters.", [nameof(SubmitDemoRequestCommand.PhoneNumber)]);
        }

        if (command.Message is not null && command.Message.Length > 2000)
        {
            yield return new ValidationResult("Message must not exceed 2000 characters.", [nameof(SubmitDemoRequestCommand.Message)]);
        }
    }
}
#pragma warning restore CA1812
