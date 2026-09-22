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

        // The only database reads in this handler. No AsNoTracking: apply mutates these
        // entities, and untracked they would silently produce no UPDATE.
        var existingPermissions = await context.Permissions.ToListAsync(cancellationToken);

        // IgnoreQueryFilters: Branches filters out RemovedAt != null, and the unique index on
        // Code is filtered the same way — so a soft-removed branch is invisible to a normal
        // query and would be re-created as a duplicate the index happily permits.
        var existingBranches = await context.Branches
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        var permissionPlan = SeedPlanner.PlanPermissions(existingPermissions, PermissionCatalog.All);
        var branchPlan = SeedPlanner.PlanBranches(existingBranches, BranchSeedData.Branches);

        // There is no code path that mutates without saving — the mutations and the save live
        // inside one branch. A preview therefore leaves the change tracker untouched, which is
        // what makes it safe to add a reconcile that deletes.
        if (command.Apply)
        {
            ApplyPermissions(permissionPlan);
            ApplyBranches(branchPlan, now);

            await context.SaveChangesAsync(cancellationToken);
        }

        return new SeedDefaultsResponse(
            command.Apply, ToResult(permissionPlan), ToResult(branchPlan));
    }

    private void ApplyPermissions(PermissionPlan plan)
    {
        foreach (var entry in plan.ToCreate)
            context.Permissions.Add(Permission.Create(
                entry.Key, entry.DisplayName, entry.Description, entry.Module));

        foreach (var update in plan.ToUpdate)
            update.Existing.Describe(
                update.Desired.DisplayName, update.Desired.Description, update.Desired.Module);
    }

    private void ApplyBranches(BranchPlan plan, DateTimeOffset now)
    {
        foreach (var create in plan.ToCreate)
            context.Branches.Add(Branch.Create(create.Code, create.Name, now));

        // Only the ones the plan says differ: Rename calls Touch unconditionally, so renaming
        // all 91 every run would destroy UpdatedAt's meaning.
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
