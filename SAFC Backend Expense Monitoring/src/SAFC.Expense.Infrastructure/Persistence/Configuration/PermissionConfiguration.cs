using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAFC.Expense.Domain.Entities;

namespace SAFC.Expense.Infrastructure.Persistence.Configuration;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Key).HasMaxLength(Permission.KeyMaxLength).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(Permission.DescriptionMaxLength).IsRequired();
        builder.Property(p => p.Module).HasMaxLength(Permission.ModuleMaxLength).IsRequired();
        builder.HasIndex(p => p.Key).IsUnique();
    }
}