using System.Reflection;
using Microsoft.EntityFrameworkCore;
using StudyPilot.Infrastructure.Persistence;
using StudyPilot.Modules.Identity.Domain;
using StudyPilot.SharedKernel.Time;

namespace StudyPilot.Modules.Identity.Persistence;

/// <summary>Persistence for the Identity module. Owns the "identity" schema.</summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options, IClock clock)
    : ModuleDbContext(options, clock)
{
    public const string SchemaName = "identity";

    public override string Schema => SchemaName;

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }
}
