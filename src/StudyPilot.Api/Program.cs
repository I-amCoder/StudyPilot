using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using StudyPilot.Infrastructure.Modules;
using StudyPilot.Infrastructure.Persistence;
using StudyPilot.Modules.Academic;
using StudyPilot.Modules.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// Database settings are bound and validated before any module context is resolved (ADR-006).
builder.Services.AddDatabaseInfrastructure(builder.Configuration);

// The composition root is the only place that knows the module set (ADR-001).
builder.Services.AddModules(
    builder.Configuration,
    new IdentityModule(),
    new AcademicModule());

var app = builder.Build();

// Migrating on startup is opt-in. Schema changes should be a deliberate step, not a side effect
// of a deploy, so the default leaves the database untouched.
var databaseOptions = app.Services
    .GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>().Value;

if (databaseOptions.MigrateOnStartup)
{
    using var scope = app.Services.CreateScope();
    var migrator = scope.ServiceProvider.GetRequiredService<IDatabaseMigrator>();
    await migrator.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Liveness: is the process up? Deliberately runs no checks — a dependency being down must not
// cause an orchestrator to kill an otherwise healthy process.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

// Readiness: can we actually serve traffic? Includes every module's database check.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
});

// Reports the composed module set — useful in diagnostics and asserted by the architecture tests.
app.MapGet("/modules", (ModuleRegistry registry) => Results.Ok(registry.Names))
    .WithTags("Platform");

app.MapModuleEndpoints();

await app.RunAsync();

/// <summary>Exposed so the test host can reference this assembly.</summary>
public partial class Program;
