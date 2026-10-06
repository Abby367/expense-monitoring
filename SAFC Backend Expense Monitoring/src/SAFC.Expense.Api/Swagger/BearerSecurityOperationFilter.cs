using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SAFC.Expense.Api.Swagger;

public sealed class BearerSecurityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.MethodInfo;

        if (method.GetCustomAttributes(true).OfType<IAllowAnonymous>().Any())
            return;

        var requiresAuth =
            method.GetCustomAttributes(true).OfType<IAuthorizeData>().Any()
            || (method.DeclaringType?.GetCustomAttributes(true).OfType<IAuthorizeData>().Any() ?? false);

        if (!requiresAuth)
            return;

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            }] = Array.Empty<string>()
        });
    }
}
