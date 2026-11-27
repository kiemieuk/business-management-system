namespace BMS.BuildingBlocks.Domain.Exceptions;

/// <summary>
/// Raised when a business rule is violated. Translated to HTTP 400 by the web layer.
/// </summary>
public class DomainException(string message) : Exception(message);
