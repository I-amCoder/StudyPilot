using Microsoft.EntityFrameworkCore;
using StudyPilot.Infrastructure.Persistence;
using StudyPilot.SharedKernel.Time;

namespace StudyPilot.Modules.Identity.Persistence;

/// <summary>
/// Persistence for the Identity module. Entities arrive with SP-144 (auth) and SP-145 (profile);
/// the context and its schema exist now so migrations stay module-local from the first one.
/// </summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options, IClock clock)
    : ModuleDbContext(options, clock)
{
    public const string SchemaName = "identity";

    public override string Schema => SchemaName;
}
