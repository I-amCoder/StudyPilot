using System.ComponentModel.DataAnnotations;

namespace StudyPilot.Infrastructure.Persistence;

/// <summary>Database settings bound from the "Database" configuration section.</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Npgsql connection string. Supplied per environment; never committed (see SP-142).</summary>
    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Transient-failure retries handled by the Npgsql execution strategy.</summary>
    [Range(0, 10)]
    public int MaxRetryCount { get; set; } = 3;

    [Range(1, 300)]
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Applies pending migrations during startup. Off by default: migrating implicitly on boot
    /// makes schema changes a side effect of a deploy rather than a deliberate step.
    /// </summary>
    public bool MigrateOnStartup { get; set; }

    /// <summary>Emits parameter values in logs. Off by default — parameters carry personal data.</summary>
    public bool EnableSensitiveDataLogging { get; set; }
}
