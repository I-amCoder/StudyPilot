using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudyPilot.SharedKernel.Time;
using StudyPilot.Infrastructure.Time;

namespace StudyPilot.Infrastructure.Persistence;

/// <summary>Registers module persistence against PostgreSQL (ADR-006).</summary>
public static class PersistenceExtensions
{
    /// <summary>Binds and validates <see cref="DatabaseOptions"/>. Called once by the host.</summary>
    public static IServiceCollection AddDatabaseInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            // Fail at startup rather than on the first query: a missing connection string should
            // stop a deploy, not surface as a runtime error under load.
            .ValidateOnStart();

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IDatabaseMigrator, DatabaseMigrator>();

        return services;
    }

    /// <summary>
    /// Registers one module's <see cref="ModuleDbContext"/>. Each context keeps its migration
    /// history inside its own schema so modules can migrate independently.
    /// </summary>
    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services,
        string schema)
        where TContext : ModuleDbContext
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);

        services.AddDbContext<TContext>((provider, builder) =>
        {
            var options = provider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>()
                .Value;

            builder.UseNpgsql(options.ConnectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable(HistoryTableName, schema);
                npgsql.CommandTimeout(options.CommandTimeoutSeconds);

                if (options.MaxRetryCount > 0)
                {
                    npgsql.EnableRetryOnFailure(options.MaxRetryCount);
                }
            });

            if (options.EnableSensitiveDataLogging)
            {
                builder.EnableSensitiveDataLogging();
            }
        });

        // One health check per module schema, so a partial outage names the module at fault.
        // Tagged "ready": the database being unreachable means we cannot serve traffic, but the
        // process is still alive and must not be restarted by a liveness probe.
        services.AddHealthChecks()
            .AddDbContextCheck<TContext>($"db:{schema}", tags: ["ready", "db"]);

        services.AddSingleton(new ModuleSchema(typeof(TContext), schema));

        return services;
    }

    internal const string HistoryTableName = "__EFMigrationsHistory";
}

/// <summary>Records which schema a module context owns, so collisions can be detected.</summary>
public sealed record ModuleSchema(Type ContextType, string Schema);
