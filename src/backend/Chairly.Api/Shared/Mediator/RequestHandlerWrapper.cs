#pragma warning disable CA1812 // Instantiated via Activator.CreateInstance in Mediator
using Microsoft.Extensions.DependencyInjection;

namespace Chairly.Api.Dispatching;

internal sealed class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerWrapperBase<TResponse>
    where TRequest : IRequest<TResponse>
{
    public override Task<TResponse> Handle(
        IRequest<TResponse> request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse().ToList();

        RequestHandlerDelegate<TResponse> pipeline = () => handler.Handle((TRequest)request, cancellationToken);

        foreach (var behavior in behaviors)
        {
            var captured = pipeline;
            var current = behavior;
            pipeline = () => current.Handle((TRequest)request, captured, cancellationToken);
        }

        return pipeline();
    }
}
#pragma warning restore CA1812
