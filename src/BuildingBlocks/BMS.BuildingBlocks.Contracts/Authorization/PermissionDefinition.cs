namespace BMS.BuildingBlocks.Contracts.Authorization;

/// <summary>
/// Describes a single permission that can be granted to a role.
/// </summary>
/// <param name="Code">Unique, stable code (e.g. <c>identity.users.view</c>). Stored in the database and issued as a JWT claim.</param>
/// <param name="Name">Human readable name shown in the UI.</param>
/// <param name="Group">Logical group (usually the module/resource) used to organise permissions in the UI.</param>
/// <param name="Description">Optional description.</param>
public sealed record PermissionDefinition(string Code, string Name, string Group, string? Description = null);
