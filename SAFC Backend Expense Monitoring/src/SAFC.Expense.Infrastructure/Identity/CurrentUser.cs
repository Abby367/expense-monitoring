using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SAFC.Expense.Application.Common.Interfaces;

namespace SAFC.Expense.Infrastructure.Identity;

internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    // Claim type is provisional: revisit when the JWT scheme is registered.
    // If the token uses "sub", or is Entra's own, this silently returns null.

    public Guid? UserId =>
        Guid.TryParse(
            accessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            out var id)
                ? id
                : null;

    public bool IsAuthenticated =>
        accessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
}
