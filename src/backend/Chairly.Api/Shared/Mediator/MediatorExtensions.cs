using Microsoft.Extensions.DependencyInjection;

namespace Chairly.Api.Dispatching;

internal static class MediatorExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection services)
    {
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        var assembly = typeof(MediatorExtensions).Assembly;

        var handlerRegistrations = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                .Select(i => new { Implementation = t, Interface = i }));

        foreach (var registration in handlerRegistrations)
        {
            services.AddScoped(registration.Interface, registration.Implementation);
        }

        return services;
    }
}
