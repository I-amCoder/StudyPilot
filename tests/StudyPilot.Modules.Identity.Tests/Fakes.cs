using StudyPilot.Modules.Identity.Authentication;
using StudyPilot.Modules.Identity.Domain;
using StudyPilot.SharedKernel.Time;

namespace StudyPilot.Modules.Identity.Tests;

/// <summary>In-memory user store, so credential logic is tested without a database.</summary>
public sealed class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = [];

    public int SaveCount { get; private set; }

    /// <summary>Simulates the unique index rejecting a concurrent insert.</summary>
    public bool ThrowDuplicateOnSave { get; set; }

    public IReadOnlyList<User> Users => _users;

    public Task<User?> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.SingleOrDefault(u => u.NormalizedEmail == email.Normalized));

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.SingleOrDefault(u => u.Id == id));

    public Task<bool> EmailExistsAsync(EmailAddress email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.Any(u => u.NormalizedEmail == email.Normalized));

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;

        if (ThrowDuplicateOnSave)
        {
            throw new DuplicateEmailException(new InvalidOperationException("unique index"));
        }

        return Task.CompletedTask;
    }
}

public sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = now;
}

/// <summary>A hasher whose verification outcome the test controls.</summary>
public sealed class StubPasswordHasher(PasswordVerificationOutcome outcome) : IPasswordHasher
{
    public int VerifyCallCount { get; private set; }

    public string Hash(string password) => $"hashed:{password}";

    public PasswordVerificationOutcome Verify(string hash, string providedPassword)
    {
        VerifyCallCount++;
        return outcome;
    }
}
