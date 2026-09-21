using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SAFC.Expense.Application.Users.CreateUser;
using SAFC.Expense.Application.Utilities.SeedDefaults;

namespace SAFC.Expense.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
    
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        
        services.AddScoped<CreateUserHandler>();
        services.AddScoped<SeedDefaultsHandler>();


        return services;

        
    }
}
