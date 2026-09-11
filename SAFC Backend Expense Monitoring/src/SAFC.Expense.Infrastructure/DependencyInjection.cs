using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SAFC.Expense.Application.Common.Interfaces;
using SAFC.Expense.Infrastructure.Persistence;

namespace SAFC.Expense.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ExpenseDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IExpenseDbContext>(sp =>
            sp.GetRequiredService<ExpenseDbContext>());

        return services;
    }
}
