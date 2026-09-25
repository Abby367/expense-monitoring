using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SAFC.Expense.Application.Common.Interfaces;

namespace SAFC.Expense.Infrastructure.Identity;

internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId => SafcClaims.ReadUserId(accessor.HttpContext?.User);


    public bool IsAuthenticated =>
        accessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
}
