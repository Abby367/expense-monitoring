using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SAFC.Expense.Application.Common.Interfaces;
using SAFC.Expense.Infrastructure.Authentication;
using SAFC.Expense.Infrastructure.Identity;
using SAFC.Expense.Infrastructure.Persistence;

namespace SAFC.Expense.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found. "
        + "Set ConnectionStrings__DefaultConnection in src/SAFC.Expense.Api/.env");


        services.AddDbContext<ExpenseDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IExpenseDbContext>(sp =>
            sp.GetRequiredService<ExpenseDbContext>());
            
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        services.AddSingleton<ITemporaryPasswordGenerator, TemporaryPasswordGenerator>();

        services.AddHttpContextAccessor();
        
        services.AddScoped<ICurrentUser, CurrentUser>();
                services.AddMemoryCache();

        services.Configure<AuthorizationCacheOptions>(
            configuration.GetSection(AuthorizationCacheOptions.SectionName));


        services.AddScoped<UserAuthorizationProvider>();

        services.AddScoped<IUserAuthorizationProvider>(sp =>
            new CachedUserAuthorizationProvider(
                sp.GetRequiredService<UserAuthorizationProvider>(),
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<IOptions<AuthorizationCacheOptions>>()));



        return services;
    }
}
