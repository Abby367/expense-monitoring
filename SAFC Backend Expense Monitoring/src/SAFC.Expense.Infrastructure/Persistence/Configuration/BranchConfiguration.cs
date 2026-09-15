

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAFC.Expense.Domain.Entities;


namespace SAFC.Expense.Infrastructure.Persistence.Configuration;

internal sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .ValueGeneratedNever();

        builder.Property(b => b.Code)
            .HasMaxLength(Branch.CodeMaxLength)
            .IsRequired();

        builder.Property(b => b.Name)
            .HasMaxLength(Branch.NameMaxLength)
            .IsRequired();

        builder.Property(b => b.RemovedReason)
            .HasMaxLength(Branch.RemovedReasonMaxLength);

        builder.HasIndex(b => b.Code)
            .IsUnique()
            .HasFilter("\"RemovedAt\" IS NULL");

        builder.HasQueryFilter(b => b.RemovedAt == null);
    }
}