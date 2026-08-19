using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using StudyPilot.Infrastructure.Persistence;

namespace StudyPilot.Infrastructure.Tests;

/// <summary>Migration-run behaviour, including the guards that protect against data loss.</summary>
[Collection(PostgresCollection.Name)]
public class DatabaseMigratorTests(PostgresFixture postgres)
{
    private static DatabaseMigrator CreateMigrator(
        IServiceProvider provider,
        params ModuleSchema[] schemas) =>
        new(provider, schemas, NullLogger<DatabaseMigrator>.Instance);

    [Fact]
    public async Task Two_modules_claiming_the_same_schema_is_rejected()
    {
        // Sharing a schema would interleave migration histories and let one module's migration
        // drop another module's table.
        var migrator = CreateMigrator(
            new ServiceCollection().BuildServiceProvider(),
            new ModuleSchema(typeof(TestModuleDbContext), "shared"),
            new ModuleSchema(typeof(OtherModuleDbContext), "shared"));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => migrator.MigrateAsync());

        Assert.Contains("schema collision", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("shared", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Schema_names_collide_case_insensitively()
    {
        // PostgreSQL folds unquoted identifiers to lower case, so "Identity" and "identity" are
        // the same schema in practice.
        var migrator = CreateMigrator(
            new ServiceCollection().BuildServiceProvider(),
            new ModuleSchema(typeof(TestModuleDbContext), "Identity"),
            new ModuleSchema(typeof(OtherModuleDbContext), "identity"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => migrator.MigrateAsync());
    }

    [Fact]
    public async Task A_context_with_no_migrations_reports_nothing_applied()
    {
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var services = new ServiceCollection();
        services.AddSingleton(postgres.CreateContext<TestModuleDbContext>(clock));

        await using var provider = services.BuildServiceProvider();
        var migrator = CreateMigrator(
            provider,
            new ModuleSchema(typeof(TestModuleDbContext), TestModuleDbContext.SchemaName));

        var outcomes = await migrator.MigrateAsync();

        var outcome = Assert.Single(outcomes);
        Assert.Equal(TestModuleDbContext.SchemaName, outcome.Schema);
        Assert.Empty(outcome.AppliedMigrations);
    }

    [Fact]
    public async Task A_registered_type_that_is_not_a_DbContext_is_rejected()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(string), "not a context");

        await using var provider = services.BuildServiceProvider();
        var migrator = CreateMigrator(provider, new ModuleSchema(typeof(string), "bogus"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => migrator.MigrateAsync());
    }
}
