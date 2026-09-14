using Microsoft.EntityFrameworkCore;
using SAFC.Expense.Application.Common.Interfaces;
using SAFC.Expense.Domain.Entities;

namespace SAFC.Expense.Infrastructure.Persistence;

public sealed class ExpenseDbContext(DbContextOptions<ExpenseDbContext> options)
    : DbContext(options), IExpenseDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ExpenseDbContext).Assembly);
    }

}