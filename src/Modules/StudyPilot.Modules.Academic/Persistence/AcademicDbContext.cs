using Microsoft.EntityFrameworkCore;
using StudyPilot.Infrastructure.Persistence;
using StudyPilot.SharedKernel.Time;

namespace StudyPilot.Modules.Academic.Persistence;

/// <summary>
/// Persistence for the Academic module (Epic SP-2). Owns its own schema so its migrations never
/// interleave with another module's.
/// </summary>
public sealed class AcademicDbContext(DbContextOptions<AcademicDbContext> options, IClock clock)
    : ModuleDbContext(options, clock)
{
    public const string SchemaName = "academic";

    public override string Schema => SchemaName;
}
