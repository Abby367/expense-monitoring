using SAFC.Expense.Domain.Authorization;
using SAFC.Expense.Domain.Entities;

namespace SAFC.Expense.Application.Utilities.SeedDefaults;


internal static class SeedPlanner
{
    internal static PermissionPlan PlanPermissions(
        IReadOnlyList<Permission> existing,
        IReadOnlyList<PermissionCatalog.Entry> catalogue)
    {
        var byKey = existing.ToDictionary(permission => permission.Key, StringComparer.Ordinal);

        var toCreate = new List<PermissionCatalog.Entry>();
        var toUpdate = new List<PermissionUpdate>();
        var unchanged = 0;

        foreach (var entry in catalogue)
        {
            if (!byKey.TryGetValue(entry.Key, out var permission))
            {
                toCreate.Add(entry);
                continue;
            }

            
            var differs =
                permission.DisplayName != entry.DisplayName ||
                permission.Description != entry.Description ||
                permission.Module != entry.Module;

            if (differs)
                toUpdate.Add(new PermissionUpdate(permission, entry));
            else
                unchanged++;
        }

        var catalogued = catalogue.Select(entry => entry.Key).ToHashSet(StringComparer.Ordinal);

        var orphaned = existing
            .Where(permission => !catalogued.Contains(permission.Key))
            .Select(permission => permission.Key)
            .ToList();

        return new PermissionPlan(toCreate, toUpdate, orphaned, unchanged);
    }

    internal static BranchPlan PlanBranches(
        IReadOnlyList<Branch> existing,
        IReadOnlyList<BranchSeedData.BranchSeedRecord> seed)
    {

        var liveByCode = existing
            .Where(branch => !branch.IsRemoved)
            .ToDictionary(branch => branch.Code, StringComparer.Ordinal);

        var removedCodes = existing
            .Where(branch => branch.IsRemoved)
            .Select(branch => branch.Code)
            .ToHashSet(StringComparer.Ordinal);

        var toCreate = new List<BranchCreate>();
        var toRename = new List<BranchRename>();
        var skippedRemoved = new List<string>();
        var seedCodes = new HashSet<string>(StringComparer.Ordinal);
        var unchanged = 0;

        foreach (var record in seed)
        {
            var code = Branch.NormalizeCode(record.Code);
            seedCodes.Add(code);

            if (liveByCode.TryGetValue(code, out var branch))
            {
                if (branch.Name != record.Name)
                    toRename.Add(new BranchRename(branch, record.Name));
                else
                    unchanged++;

                continue;
            }

            if (removedCodes.Contains(code))
            {
                skippedRemoved.Add(code);
                continue;
            }

            toCreate.Add(new BranchCreate(code, record.Name));
        }

       
        var orphaned = existing
            .Where(branch => !branch.IsRemoved && !seedCodes.Contains(branch.Code))
            .Select(branch => branch.Code)
            .ToList();

        return new BranchPlan(toCreate, toRename, skippedRemoved, orphaned, unchanged);
    }
}

internal sealed record PermissionPlan(
    IReadOnlyList<PermissionCatalog.Entry> ToCreate,
    IReadOnlyList<PermissionUpdate> ToUpdate,
    IReadOnlyList<string> Orphaned,
    int Unchanged);

internal sealed record PermissionUpdate(Permission Existing, PermissionCatalog.Entry Desired);

internal sealed record BranchPlan(
    IReadOnlyList<BranchCreate> ToCreate,
    IReadOnlyList<BranchRename> ToRename,
    IReadOnlyList<string> SkippedRemoved,
    IReadOnlyList<string> Orphaned,
    int Unchanged);

internal sealed record BranchCreate(string Code, string Name);

internal sealed record BranchRename(Branch Existing, string DesiredName);
