namespace BMS.BuildingBlocks.Domain.Exceptions;

/// <summary>
/// Raised when input is invalid. Translated to HTTP 400 (validation problem details) by the web layer.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>(errors);
    }

    public ValidationException(string field, string error)
        : this(new Dictionary<string, string[]> { [field] = [error] })
    {
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
