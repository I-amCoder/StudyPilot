namespace StudyPilot.Infrastructure.Persistence;

/// <summary>Applies pending schema migrations for every registered module context.</summary>
public interface IDatabaseMigrator
{
    /// <summary>
    /// Applies outstanding migrations. Throws if two modules claim the same schema, and
    /// propagates any migration failure rather than continuing with a partly-migrated database.
    /// </summary>
    Task<IReadOnlyList<MigrationOutcome>> MigrateAsync(CancellationToken cancellationToken = default);
}

/// <summary>What happened to one module's schema during a migration run.</summary>
public sealed record MigrationOutcome(string Schema, Type ContextType, IReadOnlyList<string> AppliedMigrations);
