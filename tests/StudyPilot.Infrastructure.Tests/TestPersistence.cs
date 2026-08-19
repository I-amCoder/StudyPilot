using Microsoft.EntityFrameworkCore;
using StudyPilot.Infrastructure.Persistence;
using StudyPilot.SharedKernel.Domain;
using StudyPilot.SharedKernel.Time;

namespace StudyPilot.Infrastructure.Tests;

/// <summary>
/// A minimal aggregate used to exercise the persistence infrastructure. It lives in the test
/// project on purpose: the shared infrastructure must contain no domain types (SP-140 AC3), and
/// the real module entities arrive with SP-144 and SP-145.
/// </summary>
public sealed class TestRecord : Entity<Guid>, IAggregateRoot, IAuditable
{
    public TestRecord(Guid id, string name)
        : base(id) => Name = name;

    private TestRecord()
    {
    }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }
}

/// <summary>A module context standing in for a real module, owning schema "testing".</summary>
public sealed class TestModuleDbContext(DbContextOptions<TestModuleDbContext> options, IClock clock)
    : ModuleDbContext(options, clock)
{
    public const string SchemaName = "testing";

    public override string Schema => SchemaName;

    public DbSet<TestRecord> Records => Set<TestRecord>();
}

/// <summary>A second module context, used to prove schemas stay isolated.</summary>
public sealed class OtherModuleDbContext(DbContextOptions<OtherModuleDbContext> options, IClock clock)
    : ModuleDbContext(options, clock)
{
    public const string SchemaName = "other_testing";

    public override string Schema => SchemaName;

    public DbSet<TestRecord> Records => Set<TestRecord>();
}

/// <summary>A clock the tests can move, so timestamp behaviour is asserted rather than guessed.</summary>
public sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = now;
}
