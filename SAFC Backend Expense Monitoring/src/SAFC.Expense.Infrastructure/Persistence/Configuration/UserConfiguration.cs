using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAFC.Expense.Domain.Entities;
namespace SAFC.Expense.Infrastructure.Persistence.Configuration;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedNever();

        builder.Property(u => u.Email)
            .HasMaxLength(User.EmailMaxLength)
            .IsRequired();

        builder.Property(u => u.FullName)
            .HasMaxLength(User.FullNameMaxLength)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(255);

        builder.Property(u => u.MicrosoftId)
            .HasMaxLength(100);

        builder.Property(u => u.RemovedReason)
            .HasMaxLength(User.RemovedReasonMaxLength);

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter("\"RemovedAt\" IS NULL");

        builder.HasIndex(u => u.MicrosoftId)
            .IsUnique()
            .HasFilter("\"MicrosoftId\" IS NOT NULL AND \"RemovedAt\" IS NULL");

        builder.HasQueryFilter(u => u.RemovedAt == null);
    }
}