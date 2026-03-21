using System.ComponentModel.DataAnnotations;

#pragma warning disable CA1812 // Instantiated via validation pipeline
namespace Chairly.Api.Features.Onboarding.SubmitSignUpRequest;

internal sealed class SubmitSignUpRequestValidator
{
    public static IEnumerable<ValidationResult> Validate(SubmitSignUpRequestCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.SalonName))
        {
            yield return new ValidationResult("SalonName is required.", [nameof(SubmitSignUpRequestCommand.SalonName)]);
        }
        else if (command.SalonName.Length > 200)
        {
            yield return new ValidationResult("SalonName must not exceed 200 characters.", [nameof(SubmitSignUpRequestCommand.SalonName)]);
        }

        if (string.IsNullOrWhiteSpace(command.OwnerFirstName))
        {
            yield return new ValidationResult("OwnerFirstName is required.", [nameof(SubmitSignUpRequestCommand.OwnerFirstName)]);
        }
        else if (command.OwnerFirstName.Length > 100)
        {
            yield return new ValidationResult("OwnerFirstName must not exceed 100 characters.", [nameof(SubmitSignUpRequestCommand.OwnerFirstName)]);
        }

        if (string.IsNullOrWhiteSpace(command.OwnerLastName))
        {
            yield return new ValidationResult("OwnerLastName is required.", [nameof(SubmitSignUpRequestCommand.OwnerLastName)]);
        }
        else if (command.OwnerLastName.Length > 100)
        {
            yield return new ValidationResult("OwnerLastName must not exceed 100 characters.", [nameof(SubmitSignUpRequestCommand.OwnerLastName)]);
        }

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            yield return new ValidationResult("Email is required.", [nameof(SubmitSignUpRequestCommand.Email)]);
        }
        else
        {
            if (command.Email.Length > 256)
            {
                yield return new ValidationResult("Email must not exceed 256 characters.", [nameof(SubmitSignUpRequestCommand.Email)]);
            }

            if (!new EmailAddressAttribute().IsValid(command.Email))
            {
                yield return new ValidationResult("Email must be a valid email address.", [nameof(SubmitSignUpRequestCommand.Email)]);
            }
        }

        if (command.PhoneNumber is not null && command.PhoneNumber.Length > 50)
        {
            yield return new ValidationResult("PhoneNumber must not exceed 50 characters.", [nameof(SubmitSignUpRequestCommand.PhoneNumber)]);
        }
    }
}
#pragma warning restore CA1812
