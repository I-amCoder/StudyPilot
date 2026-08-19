using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using StudyPilot.SharedKernel.Time;
using Testcontainers.PostgreSql;

namespace StudyPilot.Infrastructure.Tests;

/// <summary>
/// Starts a real PostgreSQL container for the persistence tests. The infrastructure under test is
/// PostgreSQL-specific — schemas, the xmin concurrency token — so an in-memory provider would
/// prove nothing.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("studypilot_tests")
        .WithUsername("studypilot")
        .WithPassword("studypilot")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>
    /// Creates this context's schema and tables. EnsureCreated is unusable here: it no-ops once
    /// the database holds any table, so a second module's schema would never be created.
    /// </summary>
    public static async Task EnsureTablesAsync(DbContext context)
    {
        var creator = (RelationalDatabaseCreator)context.GetService<IDatabaseCreator>();

        try
        {
            await creator.CreateTablesAsync();
        }
        catch (PostgresException ex) when (ex.SqlState is "42P07" or "42P06")
        {
            // Duplicate table or schema: another test in this collection already created them.
        }
    }

    public TContext CreateContext<TContext>(IClock clock)
        where TContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TContext>().UseNpgsql(ConnectionString);

        return (TContext)Activator.CreateInstance(typeof(TContext), builder.Options, clock)!;
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
