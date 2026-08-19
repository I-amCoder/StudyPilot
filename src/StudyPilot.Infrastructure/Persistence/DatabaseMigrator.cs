using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StudyPilot.Infrastructure.Persistence;

/// <inheritdoc />
public sealed class DatabaseMigrator(
    IServiceProvider provider,
    IEnumerable<ModuleSchema> moduleSchemas,
    ILogger<DatabaseMigrator> logger) : IDatabaseMigrator
{
    public async Task<IReadOnlyList<MigrationOutcome>> MigrateAsync(
        CancellationToken cancellationToken = default)
    {
        var schemas = moduleSchemas.ToArray();
        GuardAgainstSchemaCollisions(schemas);

        var outcomes = new List<MigrationOutcome>(schemas.Length);

        foreach (var module in schemas)
        {
            if (provider.GetRequiredService(module.ContextType) is not DbContext context)
            {
                throw new InvalidOperationException(
                    $"{module.ContextType.Name} is registered as a module context but is not a DbContext.");
            }

            var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken))
                .ToArray();

            if (pending.Length == 0)
            {
                logger.LogInformation("Schema {Schema}: already up to date.", module.Schema);
                outcomes.Add(new MigrationOutcome(module.Schema, module.ContextType, []));
                continue;
            }

            logger.LogInformation(
                "Schema {Schema}: applying {Count} migration(s): {Migrations}",
                module.Schema, pending.Length, string.Join(", ", pending));

            // Deliberately not wrapped in try/catch: a failed migration must surface and stop the
            // run. Swallowing it would leave later modules migrating against a broken schema.
            await context.Database.MigrateAsync(cancellationToken);

            outcomes.Add(new MigrationOutcome(module.Schema, module.ContextType, pending));
        }

        return outcomes;
    }

    private static void GuardAgainstSchemaCollisions(IReadOnlyList<ModuleSchema> schemas)
    {
        var collisions = schemas
            .GroupBy(s => s.Schema, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} claimed by {string.Join(" and ", g.Select(x => x.ContextType.Name))}")
            .ToArray();

        if (collisions.Length > 0)
        {
            // Two modules sharing a schema would interleave their migration histories and let one
            // module's migration drop another's table.
            throw new InvalidOperationException(
                $"Module schema collision: {string.Join("; ", collisions)}.");
        }
    }
}
