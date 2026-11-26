namespace BMS.BuildingBlocks.Domain.Exceptions;

/// <summary>
/// Raised when a requested resource does not exist. Translated to HTTP 404 by the web layer.
/// </summary>
public class NotFoundException(string resource, object key)
    : Exception($"{resource} '{key}' was not found.");
