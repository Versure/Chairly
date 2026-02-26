#pragma warning disable CA1711 // 'Delegate' suffix is accurate for a delegate type
namespace Chairly.Api.Dispatching;

internal delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
#pragma warning restore CA1711
