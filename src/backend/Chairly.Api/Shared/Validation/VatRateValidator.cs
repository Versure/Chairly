using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Shared.Validation;

internal static class VatRateValidator
{
    private static readonly decimal[] _validRates = [0m, 9m, 21m];

    public static void Validate(decimal? vatRate)
    {
        if (vatRate is null)
        {
            return;
        }

        if (!Array.Exists(_validRates, r => r == vatRate.Value))
        {
            throw new ValidationException(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                { "VatRate", ["VatRate must be one of: 0, 9, 21."] },
            });
        }
    }

    public static void ValidateRequired(decimal vatRate)
    {
        if (!Array.Exists(_validRates, r => r == vatRate))
        {
            throw new ValidationException(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                { "DefaultVatRate", ["DefaultVatRate must be one of: 0, 9, 21."] },
            });
        }
    }
}
