namespace BMS.BuildingBlocks.Domain.Entities;

/// <summary>
/// Base class for every domain entity. Entities are compared by identity (Id), not by value.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public override bool Equals(object? obj) =>
        obj is Entity other && other.GetType() == GetType() && other.Id == Id;

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
