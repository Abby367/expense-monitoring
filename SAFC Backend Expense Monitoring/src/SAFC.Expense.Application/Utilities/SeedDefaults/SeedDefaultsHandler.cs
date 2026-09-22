using Microsoft.EntityFrameworkCore;
using SAFC.Expense.Application.Common.Interfaces;
using SAFC.Expense.Domain.Authorization;
using SAFC.Expense.Domain.Entities;
using static SAFC.Expense.Application.Utilities.SeedDefaults.SeedPlanner;

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

        var rolePlan = SeedPlanner.PlanRoles(
            existingRoles, existingPermissions, SystemRoleDefinitions.All);
        
        var permissionPlan = SeedPlanner.PlanPermissions(existingPermissions, PermissionCatalog.All);
        var branchPlan = SeedPlanner.PlanBranches(existingBranches, BranchSeedData.Branches);

        if (command.Apply)
        {
            var permissionIdsByKey = ApplyPermissions(permissionPlan, existingPermissions);
            ApplyBranches(branchPlan, now);
            ApplyRoles(rolePlan, permissionIdsByKey, now);

            await context.SaveChangesAsync(cancellationToken);
        }

        return new SeedDefaultsResponse(
            command.Apply, ToResult(permissionPlan), ToResult(branchPlan), ToResult(rolePlan));

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

    private void ApplyRoles(
        RolePlan plan, IReadOnlyDictionary<string, Guid> permissionIdsByKey, DateTimeOffset now)
    {
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
        }

        foreach (var sync in plan.ToSync)
        {
            var permissionIds = sync.Desired.PermissionKeys
                .Select(key => permissionIdsByKey[key])
                .ToList();

            sync.Existing.SyncSystemDefinition(
                sync.Desired.Name, sync.Desired.Description, permissionIds, null, now);
        }
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
}
