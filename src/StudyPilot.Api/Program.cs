using StudyPilot.Infrastructure.Modules;
using StudyPilot.Modules.Academic;
using StudyPilot.Modules.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// The composition root is the only place that knows the module set (ADR-001).
builder.Services.AddModules(
    builder.Configuration,
    new IdentityModule(),
    new AcademicModule());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");

// Reports the composed module set — useful in diagnostics and asserted by the architecture tests.
app.MapGet("/modules", (ModuleRegistry registry) => Results.Ok(registry.Names))
    .WithTags("Platform");

app.MapModuleEndpoints();

app.Run();

/// <summary>Exposed so the test host can reference this assembly.</summary>
public partial class Program;
