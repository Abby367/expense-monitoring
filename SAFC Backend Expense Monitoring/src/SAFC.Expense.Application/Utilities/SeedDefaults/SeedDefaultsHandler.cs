using Microsoft.EntityFrameworkCore;
using SAFC.Expense.Application.Common.Interfaces;
using SAFC.Expense.Domain.Authorization;
using SAFC.Expense.Domain.Entities;


namespace SAFC.Expense.Application.Utilities.SeedDefaults;

public sealed class SeedDefaultsHandler(IExpenseDbContext context)
{
    public async Task<SeedDefaultsResponse> Handle(
        SeedDefaultsCommand command, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;


        var existingPermissions = await context.Permissions.ToListAsync(cancellationToken);


        var existingRoles = await context.Roles
            .IgnoreQueryFilters()
            .Include(role => role.RolePermissions)
            .ToListAsync(cancellationToken);
        
        var existingBranches = await context.Branches
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        // No IgnoreQueryFilters here, deliberately, unlike the two reads above: a soft-removed
        // user must come back as UserNotFound rather than be handed org-wide SUPERADMIN.
        User? superAdminUser = null;

        if (!string.IsNullOrWhiteSpace(command.SuperAdminEmail))
        {
            var email = User.NormalizeEmail(command.SuperAdminEmail);

            superAdminUser = await context.Users
                .Include(user => user.UserRoles)
                .FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
        }

        var rolePlan = SeedPlanner.PlanRoles(
            existingRoles, existingPermissions, SystemRoleDefinitions.All);

        var grantPlan = SeedPlanner.PlanGrant(
            command.SuperAdminEmail,
            superAdminUser,
            existingRoles.FirstOrDefault(role =>
                role.Code == RoleCodes.SuperAdmin && role.RemovedAt is null),
            // PlanRoles has three ways a definition ends up with no usable live role:
            // Conflicts, ToCreate, and SkippedRemoved. ToCreate is fine — ApplyRoles creates
            // the row and puts it in the map. The other two mean no row exists to grant
            // against, and ApplyGrant's indexer would throw and roll back the whole run.
            rolePlan.Conflicts.Contains(RoleCodes.SuperAdmin)
                || rolePlan.SkippedRemoved.Contains(RoleCodes.SuperAdmin));

        var permissionPlan = SeedPlanner.PlanPermissions(existingPermissions, PermissionCatalog.All);
        var branchPlan = SeedPlanner.PlanBranches(existingBranches, BranchSeedData.Branches);


        if (command.Apply)
        {
            var permissionIdsByKey = ApplyPermissions(permissionPlan, existingPermissions);
            ApplyBranches(branchPlan, now);
            var rolesByCode = ApplyRoles(rolePlan, existingRoles, permissionIdsByKey, now);
            ApplyGrant(grantPlan, rolesByCode, now);


            await context.SaveChangesAsync(cancellationToken);
        }

        return new SeedDefaultsResponse(
            command.Apply,
            ToResult(permissionPlan),
            ToResult(branchPlan),
            ToResult(rolePlan),
            ToResult(grantPlan, command.SuperAdminEmail));


    }
       private Dictionary<string, Guid> ApplyPermissions(
        PermissionPlan plan, IReadOnlyList<Permission> existing)
    {
        var idsByKey = existing.ToDictionary(
            permission => permission.Key, permission => permission.Id, StringComparer.Ordinal);

        foreach (var entry in plan.ToCreate)
        {
            var permission = Permission.Create(
                entry.Key, entry.DisplayName, entry.Description, entry.Module);

            context.Permissions.Add(permission);

            idsByKey[permission.Key] = permission.Id;
        }

        foreach (var update in plan.ToUpdate)
            update.Existing.Describe(
                update.Desired.DisplayName, update.Desired.Description, update.Desired.Module);

        return idsByKey;
    }

        private Dictionary<string, Role> ApplyRoles(
            RolePlan plan,
            IReadOnlyList<Role> existing,
            IReadOnlyDictionary<string, Guid> permissionIdsByKey,
            DateTimeOffset now)
    {

        var rolesByCode = existing
            .Where(role => role.RemovedAt is null)
            .ToDictionary(role => role.Code, StringComparer.Ordinal);

        foreach (var create in plan.ToCreate)
        {
            var definition = create.Definition;

            var role = definition.IsSystem
                ? Role.CreateSystem(definition.Code, definition.Name, definition.Description, now)
                : Role.Create(definition.Code, definition.Name, definition.Description, now);

            var permissionIds = definition.PermissionKeys
                .Select(key => permissionIdsByKey[key])
                .ToList();

            if (definition.IsSystem)
                role.SyncSystemDefinition(
                    definition.Name, definition.Description, permissionIds, null, now);
            else
                role.SetPermissions(permissionIds, null, now);

            context.Roles.Add(role);

            rolesByCode[role.Code] = role;
        }

        foreach (var sync in plan.ToSync)
        {
            var permissionIds = sync.Desired.PermissionKeys
                .Select(key => permissionIdsByKey[key])
                .ToList();

            sync.Existing.SyncSystemDefinition(
                sync.Desired.Name, sync.Desired.Description, permissionIds, null, now);
        }

        return rolesByCode;
    }

    private void ApplyGrant(
        GrantPlan plan, IReadOnlyDictionary<string, Role> rolesByCode, DateTimeOffset now)
    {
        if (plan.ToCreate is null)
            return;


        var role = rolesByCode[plan.ToCreate.RoleCode];

        plan.ToCreate.User.Grant(role.Id, branchId: null, grantedByUserId: null, now);
    }

    private static RoleSeedResult ToResult(RolePlan plan) =>
        new(plan.ToCreate.Select(create => create.Code).ToList(),
            plan.ToSync.Select(sync => sync.Existing.Code).ToList(),
            plan.SkippedRemoved,
            plan.Unmanaged,
            plan.Conflicts,
            plan.Unchanged);



    private void ApplyBranches(BranchPlan plan, DateTimeOffset now)
    {
        foreach (var create in plan.ToCreate)
            context.Branches.Add(Branch.Create(create.Code, create.Name, now));

        foreach (var rename in plan.ToRename)
            rename.Existing.Rename(rename.DesiredName, now);
    }

    private static PermissionSeedResult ToResult(PermissionPlan plan) =>
        new(plan.ToCreate.Select(entry => entry.Key).ToList(),
            plan.ToUpdate.Select(update => update.Existing.Key).ToList(),
            plan.Orphaned,
            plan.Unchanged);

    private static BranchSeedResult ToResult(BranchPlan plan) =>
        new(plan.ToCreate.Select(create => create.Code).ToList(),
            plan.ToRename.Select(rename => rename.Existing.Code).ToList(),
            plan.SkippedRemoved,
            plan.Orphaned,
            plan.Unchanged);
    private static GrantSeedResult ToResult(GrantPlan plan, string? email) =>
        new(plan.Outcome.ToString(), email);

}
