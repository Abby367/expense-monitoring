using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SAFC.Expense.Application.Users.CreateUser;

namespace SAFC.Expense.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
    
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        
        services.AddScoped<CreateUserHandler>();

        return services;

        
    }
}
