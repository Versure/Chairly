using System.ComponentModel.DataAnnotations;

#pragma warning disable CA1812 // Instantiated via DI (AddMediator registers it as IPipelineBehavior<,>)
namespace Chairly.Api.Shared.Mediator;

#pragma warning disable MA0026 // TODO: Consider returning validation errors via OneOf instead of exceptions (see CLAUDE.md no-exceptions rule)
#pragma warning restore MA0026
internal sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> continuation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(continuation);

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(request!);

        if (Validator.TryValidateObject(request!, validationContext, validationResults, validateAllProperties: true))
        {
            return continuation();
        }

        var errors = validationResults
            .GroupBy(vr => vr.MemberNames.FirstOrDefault() ?? string.Empty, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.Select(vr => vr.ErrorMessage ?? "Invalid value.").ToArray(),
                StringComparer.Ordinal);

        throw new ValidationException(errors);
    }
}
