namespace StudyPilot.SharedKernel.Domain;

/// <summary>
/// Base class for domain entities. Identity is by <see cref="Id"/>, not by reference.
/// </summary>
public abstract class Entity<TId>
    where TId : notnull
{
    protected Entity(TId id) => Id = id;

    /// <summary>
    /// For ORM materialisation only. EF Core rehydrates entities without running domain
    /// constructors, so it needs a parameterless entry point and a settable key; application code
    /// must use the constructor that takes an identity.
    /// </summary>
    protected Entity() => Id = default!;

    public TId Id { get; protected set; }

    public override bool Equals(object? obj) =>
        obj is Entity<TId> other && GetType() == other.GetType() && Id.Equals(other.Id);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
