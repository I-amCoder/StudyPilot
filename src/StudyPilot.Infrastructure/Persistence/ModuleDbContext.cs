using Microsoft.EntityFrameworkCore;
using StudyPilot.SharedKernel.Domain;
using StudyPilot.SharedKernel.Time;

namespace StudyPilot.Infrastructure.Persistence;

/// <summary>
/// Base context for a module's persistence. Each module owns a PostgreSQL schema, so the module
/// boundaries enforced in code (ADR-001) also hold in the database: no module can reach another's
/// tables by accident, and migrations never collide.
/// </summary>
public abstract class ModuleDbContext(DbContextOptions options, IClock clock) : DbContext(options)
{
    /// <summary>PostgreSQL's implicit row-version system column.</summary>
    internal const string ConcurrencyTokenName = "xmin";

    /// <summary>The PostgreSQL schema this module owns. Must be unique across modules.</summary>
    public abstract string Schema { get; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Aggregate roots get an optimistic-concurrency token. Without one, two concurrent
            // writers silently overwrite each other and the earlier write is lost with no error.
            if (typeof(IAggregateRoot).IsAssignableFrom(entityType.ClrType))
            {
                // PostgreSQL's xmin system column changes on every update, so it works as a
                // free row version. Npgsql 10 dropped UseXminAsConcurrencyToken(); mapping the
                // shadow property directly is the supported equivalent.
                modelBuilder.Entity(entityType.ClrType)
                    .Property<uint>(ConcurrencyTokenName)
                    .HasColumnName(ConcurrencyTokenName)
                    .HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();
            }
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampAuditTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        StampAuditTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampAuditTimestamps()
    {
        var now = clock.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = now;
                    entry.Entity.UpdatedAtUtc = null;
                    break;

                case EntityState.Modified:
                    // Creation time is immutable; reassigning it would rewrite history.
                    entry.Property(nameof(IAuditable.CreatedAtUtc)).IsModified = false;
                    entry.Entity.UpdatedAtUtc = now;
                    break;
            }
        }
    }
}
