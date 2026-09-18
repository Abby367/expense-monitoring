using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAFC.Expense.Domain.Entities;

namespace SAFC.Expense.Infrastructure.Persistence.Configuration;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Code).HasMaxLength(Role.CodeMaxLength).IsRequired();
        builder.Property(r => r.Name).HasMaxLength(Role.NameMaxLength).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(Role.DescriptionMaxLength).IsRequired();
        builder.Property(r => r.RemovedReason).HasMaxLength(Role.RemovedReasonMaxLength);

        builder.HasIndex(r => r.Code).IsUnique().HasFilter("\"RemovedAt\" IS NULL");
        builder.HasQueryFilter(r => r.RemovedAt == null);

        builder.HasMany(r => r.RolePermissions)
            .WithOne()
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
