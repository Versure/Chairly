#pragma warning disable CA1716 // 'Shared' is a VB keyword — intentional by folder convention
namespace Chairly.Api.Shared.Results;

internal readonly struct KeycloakError(string message)
{
    public string Message { get; } = message;
}
#pragma warning restore CA1716
