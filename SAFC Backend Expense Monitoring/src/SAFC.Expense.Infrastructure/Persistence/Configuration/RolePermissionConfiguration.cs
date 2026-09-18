using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAFC.Expense.Domain.Entities;

namespace SAFC.Expense.Infrastructure.Persistence.Configuration;

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.HasOne<Permission>()
            .WithMany()
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
