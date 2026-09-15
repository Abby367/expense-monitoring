using Microsoft.EntityFrameworkCore;
using SAFC.Expense.Domain.Entities;

namespace SAFC.Expense.Application.Common.Interfaces;

public interface IExpenseDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Branch> Branches { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}