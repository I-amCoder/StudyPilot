namespace StudyPilot.Infrastructure.Modules;

/// <summary>The set of modules composed into this host, resolved once at startup.</summary>
public sealed class ModuleRegistry(IReadOnlyList<IModule> modules)
{
    public IReadOnlyList<IModule> Modules { get; } = modules;

    public IEnumerable<string> Names => Modules.Select(m => m.Name);
}
