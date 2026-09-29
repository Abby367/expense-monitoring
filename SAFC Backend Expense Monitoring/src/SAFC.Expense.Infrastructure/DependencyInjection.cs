using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        IConfiguration configuration,
        IHostEnvironment environment)

    {

        string connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found. "
        + "Set ConnectionStrings__DefaultConnection in src/SAFC.Expense.Api/.env");


        // Fail fast, for the same reason as the connection string above — but the reason is
        // sharper here: a misconfigured tenant fails NOWHERE at runtime. The stranger is simply
        // denied by the authorization layer, so the mistake stays invisible until something else
        // changes.
        var configuredTenantId = configuration[$"{MicrosoftAuthOptions.SectionName}:TenantId"];
        var isDevelopment = environment.IsDevelopment();

        if (TenantPin.Validate(TenantPin.SafcTenantId, configuredTenantId, isDevelopment)
            is { } tenantError)
            throw new InvalidOperationException(tenantError);

        // Required, with no safe default. ClientId is the audience an Entra token must be
        // addressed to, so an empty one is not "no check" — it is a check nothing can pass.
        // Every sign-in is denied, for a reason that appears nowhere at startup.
        var clientId = configuration[$"{MicrosoftAuthOptions.SectionName}:ClientId"];

        if (string.IsNullOrWhiteSpace(clientId))
            throw new InvalidOperationException(
                "Entra application (client) id not found. "
                + "Set Microsoft__ClientId in src/SAFC.Expense.Api/.env");

        if (!Guid.TryParse(clientId, out _))
            throw new InvalidOperationException(
                "Microsoft__ClientId is not a GUID. An Entra application id always is, so this "
                + "is most likely the client secret pasted into the wrong key. Rotate it if so.");

        services.Configure<MicrosoftAuthOptions>(options =>
        {
            options.TenantId = TenantPin.Effective(
                TenantPin.SafcTenantId, configuredTenantId, isDevelopment);

            options.ClientId = clientId.Trim();
        });


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
