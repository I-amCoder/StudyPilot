namespace StudyPilot.Modules.Identity.Domain;

/// <summary>Persistence boundary for the <see cref="User"/> aggregate.</summary>
public interface IUserRepository
{
    Task<User?> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default);

    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(EmailAddress email, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
