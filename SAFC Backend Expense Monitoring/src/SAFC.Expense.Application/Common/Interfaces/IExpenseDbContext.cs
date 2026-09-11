using Microsoft.EntityFrameworkCore;
using SAFC.Expense.Domain.Entities;

namespace SAFC.Expense.Application.Common.Interfaces;

public interface IExpenseDbContext
{
    DbSet<User> Users { get; set;}
}