using System.Text.Json.Serialization;

namespace Chairly.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentMethod
{
    Cash = 0,
    Pin = 1,
    BankTransfer = 2,
}
