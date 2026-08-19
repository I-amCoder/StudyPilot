namespace StudyPilot.SharedKernel.Domain;

/// <summary>
/// Base class for domain entities. Identity is by <see cref="Id"/>, not by reference.
/// </summary>
public abstract class Entity<TId>
    where TId : notnull
{
    protected Entity(TId id) => Id = id;

    public TId Id { get; }

    public override bool Equals(object? obj) =>
        obj is Entity<TId> other && GetType() == other.GetType() && Id.Equals(other.Id);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
