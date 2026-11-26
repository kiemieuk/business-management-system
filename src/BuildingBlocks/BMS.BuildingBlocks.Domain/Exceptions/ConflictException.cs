namespace BMS.BuildingBlocks.Domain.Exceptions;

/// <summary>
/// Raised when an operation conflicts with the current state (e.g. duplicates). Translated to HTTP 409 by the web layer.
/// </summary>
public class ConflictException(string message) : Exception(message);
