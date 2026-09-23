using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAFC.Expense.Domain.Entities;

namespace SAFC.Expense.Infrastructure.Persistence.Configuration;

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        builder.HasKey(ur => ur.Id);
        builder.Property(ur => ur.Id).ValueGeneratedNever();

        builder.Property(ur => ur.RemovedReason).HasMaxLength(UserRole.RemovedReasonMaxLength);


        builder.Ignore(ur => ur.Scope);


        builder.HasIndex(ur => new { ur.UserId, ur.RoleId, ur.BranchId })
            .IsUnique()
            .HasFilter("\"BranchId\" IS NOT NULL AND \"RemovedAt\" IS NULL");

        builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
            .IsUnique()
            .HasFilter("\"BranchId\" IS NULL AND \"RemovedAt\" IS NULL");

        builder.HasIndex(ur => ur.UserId);


        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(ur => ur.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(ur => ur.RemovedAt == null);
    }
}
