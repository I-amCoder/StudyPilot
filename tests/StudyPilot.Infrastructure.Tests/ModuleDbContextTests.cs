using Microsoft.EntityFrameworkCore;

namespace StudyPilot.Infrastructure.Tests;

/// <summary>
/// Persistence behaviour against real PostgreSQL. Covers SP-141 AC2 (success and failure paths)
/// and AC3 (errors do not cause silent data loss).
/// </summary>
[Collection(PostgresCollection.Name)]
public class ModuleDbContextTests(PostgresFixture postgres)
{
    private static readonly DateTimeOffset Now = new(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);

    private async Task<TestModuleDbContext> CreateSchemaAsync(FixedClock clock)
    {
        var context = postgres.CreateContext<TestModuleDbContext>(clock);
        await PostgresFixture.EnsureTablesAsync(context);
        return context;
    }

    [Fact]
    public async Task Insert_stamps_created_timestamp()
    {
        var clock = new FixedClock(Now);
        await using var context = await CreateSchemaAsync(clock);

        var record = new TestRecord(Guid.NewGuid(), "first");
        context.Records.Add(record);
        await context.SaveChangesAsync();

        var stored = await context.Records.AsNoTracking().SingleAsync(r => r.Id == record.Id);

        Assert.Equal(Now, stored.CreatedAtUtc);
        Assert.Null(stored.UpdatedAtUtc);
    }

    [Fact]
    public async Task Update_stamps_updated_timestamp_and_leaves_created_untouched()
    {
        var clock = new FixedClock(Now);
        await using var context = await CreateSchemaAsync(clock);

        var record = new TestRecord(Guid.NewGuid(), "before");
        context.Records.Add(record);
        await context.SaveChangesAsync();

        clock.UtcNow = Now.AddHours(2);
        record.Name = "after";
        record.CreatedAtUtc = Now.AddYears(-10);   // an attempt to rewrite history
        await context.SaveChangesAsync();

        var stored = await context.Records.AsNoTracking().SingleAsync(r => r.Id == record.Id);

        Assert.Equal("after", stored.Name);
        Assert.Equal(Now.AddHours(2), stored.UpdatedAtUtc);
        Assert.Equal(Now, stored.CreatedAtUtc);
    }

    [Fact]
    public async Task Concurrent_update_is_rejected_rather_than_silently_overwriting()
    {
        // AC3: without a concurrency token the second writer would overwrite the first and the
        // earlier change would vanish with no error reported to anyone.
        var clock = new FixedClock(Now);
        await using var setup = await CreateSchemaAsync(clock);

        var id = Guid.NewGuid();
        setup.Records.Add(new TestRecord(id, "original"));
        await setup.SaveChangesAsync();

        await using var first = postgres.CreateContext<TestModuleDbContext>(clock);
        await using var second = postgres.CreateContext<TestModuleDbContext>(clock);

        var fromFirst = await first.Records.SingleAsync(r => r.Id == id);
        var fromSecond = await second.Records.SingleAsync(r => r.Id == id);

        fromFirst.Name = "written by first";
        await first.SaveChangesAsync();

        fromSecond.Name = "written by second";

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        await using var verify = postgres.CreateContext<TestModuleDbContext>(clock);
        var survivor = await verify.Records.AsNoTracking().SingleAsync(r => r.Id == id);
        Assert.Equal("written by first", survivor.Name);
    }

    [Fact]
    public async Task Rolled_back_transaction_leaves_no_partial_data()
    {
        var clock = new FixedClock(Now);
        await using var context = await CreateSchemaAsync(clock);

        var id = Guid.NewGuid();
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            context.Records.Add(new TestRecord(id, "doomed"));
            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        await using var verify = postgres.CreateContext<TestModuleDbContext>(clock);
        Assert.False(await verify.Records.AnyAsync(r => r.Id == id));
    }

    [Fact]
    public async Task Modules_write_to_their_own_schema_only()
    {
        var clock = new FixedClock(Now);
        await using var owned = await CreateSchemaAsync(clock);
        await using var other = postgres.CreateContext<OtherModuleDbContext>(clock);
        await PostgresFixture.EnsureTablesAsync(other);

        var id = Guid.NewGuid();
        owned.Records.Add(new TestRecord(id, "belongs to testing schema"));
        await owned.SaveChangesAsync();

        // Same entity type, same table name, different schema: the row must not be visible.
        Assert.False(await other.Records.AnyAsync(r => r.Id == id));
        Assert.Equal(TestModuleDbContext.SchemaName, owned.Schema);
        Assert.Equal(OtherModuleDbContext.SchemaName, other.Schema);
    }

    [Fact]
    public async Task Unreachable_database_surfaces_an_error_instead_of_reporting_success()
    {
        var clock = new FixedClock(Now);
        var options = new DbContextOptionsBuilder<TestModuleDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=nope;Username=nope;Password=nope;Timeout=2")
            .Options;

        await using var context = new TestModuleDbContext(options, clock);
        context.Records.Add(new TestRecord(Guid.NewGuid(), "never stored"));

        await Assert.ThrowsAnyAsync<Exception>(() => context.SaveChangesAsync());
    }
}
