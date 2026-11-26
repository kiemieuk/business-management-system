namespace BMS.BuildingBlocks.Domain.Entities;

/// <summary>
/// Entities implementing this interface get their audit columns populated automatically by the persistence layer.
/// </summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }
    string? CreatedBy { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}
