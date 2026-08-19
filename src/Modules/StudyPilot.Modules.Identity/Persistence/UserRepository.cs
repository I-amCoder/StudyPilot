using Microsoft.EntityFrameworkCore;
using Npgsql;
using StudyPilot.Modules.Identity.Authentication;
using StudyPilot.Modules.Identity.Domain;

namespace StudyPilot.Modules.Identity.Persistence;

/// <inheritdoc />
public sealed class UserRepository(IdentityDbContext context) : IUserRepository
{
    private const string UniqueViolation = "23505";

    public Task<User?> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        return context.Users
            .SingleOrDefaultAsync(user => user.NormalizedEmail == email.Normalized, cancellationToken);
    }

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<bool> EmailExistsAsync(EmailAddress email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        return context.Users.AnyAsync(user => user.NormalizedEmail == email.Normalized, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await context.Users.AddAsync(user, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: UniqueViolation } postgres
                  && postgres.ConstraintName == UserConfiguration.UniqueEmailIndexName)
        {
            // Translated at the persistence boundary so callers handle a domain conflict rather
            // than a provider-specific exception.
            throw new DuplicateEmailException(ex);
        }
    }
}
