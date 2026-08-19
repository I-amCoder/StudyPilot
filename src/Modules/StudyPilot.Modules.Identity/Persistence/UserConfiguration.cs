using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyPilot.Modules.Identity.Domain;

namespace StudyPilot.Modules.Identity.Persistence;

/// <summary>Maps <see cref="User"/> onto the identity schema.</summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public const string UniqueEmailIndexName = "ix_users_normalized_email";

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(EmailAddress.MaxLength);

        builder.Property(user => user.NormalizedEmail)
            .IsRequired()
            .HasMaxLength(EmailAddress.MaxLength);

        // The database, not application code, is what actually guarantees one account per address:
        // two concurrent registrations both pass an existence check.
        builder.HasIndex(user => user.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName(UniqueEmailIndexName);

        builder.Property(user => user.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(user => user.SecurityStamp)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(user => user.IsActive)
            .IsRequired();

        builder.Property(user => user.CreatedAtUtc)
            .IsRequired();

        builder.Property(user => user.UpdatedAtUtc);
    }
}
