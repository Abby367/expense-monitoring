using Microsoft.EntityFrameworkCore;
using SAFC.Expense.Application.Common.Authorization;
using SAFC.Expense.Application.Common.Interfaces;
using SAFC.Expense.Domain.Enums;

namespace SAFC.Expense.Infrastructure.Identity;


public sealed class UserAuthorizationProvider(IExpenseDbContext context) : IUserAuthorizationProvider
{
    public async Task<AuthorizationSnapshot> GetSnapshotAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {

        var grants = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && u.Status == UserStatus.Active)

            .SelectMany(u => u.UserRoles)


            .Where(ur => ur.BranchId == null
                      || context.Branches.Any(b => b.Id == ur.BranchId))

            .Join(context.Roles,
                  ur => ur.RoleId,
                  role => role.Id,
                  (ur, role) => new { ur.BranchId, Role = role })

            .SelectMany(
                x => x.Role.RolePermissions,
                (x, rolePermission) => new { x.BranchId, rolePermission.PermissionId })


            .Join(context.Permissions,
                  x => x.PermissionId,
                  permission => permission.Id,
                  (x, permission) => new GrantedPermission(permission.Key, x.BranchId))


            .ToListAsync(cancellationToken);

        return grants.Count == 0
            ? AuthorizationSnapshot.Empty(userId)
            : AuthorizationSnapshot.FromGrants(userId, grants);
    }
}
