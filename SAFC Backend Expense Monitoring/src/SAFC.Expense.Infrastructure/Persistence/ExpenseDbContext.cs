using Microsoft.EntityFrameworkCore;
using SAFC.Expense.Application.Common.Interfaces;
using SAFC.Expense.Domain.Entities;

namespace SAFC.Expense.Infrastructure.Persistence;
public sealed class ExpenseDbContext(DbContextOptions<ExpenseDbContext> options)
    : DbContext(options), IExpenseDbContext
{
}