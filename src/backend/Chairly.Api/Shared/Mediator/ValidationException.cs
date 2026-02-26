namespace Chairly.Api.Dispatching;

#pragma warning disable CA1032 // Custom exception with required errors parameter — standard constructors not needed
#pragma warning disable RCS1194 // Custom exception with required errors parameter — standard constructors not needed
internal sealed class ValidationException : Exception
{
    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
#pragma warning restore RCS1194
#pragma warning restore CA1032
