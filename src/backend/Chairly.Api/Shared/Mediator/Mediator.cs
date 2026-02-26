#pragma warning disable CA1812 // Instantiated via DI (AddMediator registers it)
namespace Chairly.Api.Dispatching;

internal sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var wrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(requestType, typeof(TResponse));
        var wrapper = (RequestHandlerWrapperBase<TResponse>)(Activator.CreateInstance(wrapperType)
            ?? throw new InvalidOperationException($"Could not create handler wrapper for {requestType.Name}."));

        return wrapper.Handle(request, serviceProvider, cancellationToken);
    }
}
