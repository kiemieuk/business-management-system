namespace BMS.BuildingBlocks.Domain.Exceptions;

/// <summary>
/// Raised when the current user is authenticated but not allowed to perform an operation on a specific resource.
/// Translated to HTTP 403 by the web layer.
/// </summary>
public class ForbiddenException(string message) : Exception(message);
