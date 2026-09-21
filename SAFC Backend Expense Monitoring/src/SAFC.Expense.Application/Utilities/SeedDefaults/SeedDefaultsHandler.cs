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

        var permissions = await SeedPermissionsAsync(cancellationToken);
        var branches = await SeedBranchesAsync(now, cancellationToken);


        if (command.Apply)
            await context.SaveChangesAsync(cancellationToken);

        return new SeedDefaultsResponse(command.Apply, permissions, branches);

      
    }

    private async Task<PermissionSeedResult> SeedPermissionsAsync(
        CancellationToken cancellationToken)
    {
        
        var existing = await context.Permissions.ToListAsync(cancellationToken);
        var byKey = existing.ToDictionary(p => p.Key, StringComparer.Ordinal);

        var created = new List<string>();
        var updated = new List<string>();
        var unchanged = 0;

        foreach (var entry in PermissionCatalog.All)
        {
            if (!byKey.TryGetValue(entry.Key, out var permission))
            {
                context.Permissions.Add(Permission.Create(
                    entry.Key, entry.DisplayName, entry.Description, entry.Module));
                created.Add(entry.Key);
                continue;
            }

            
            var differs =
                permission.DisplayName != entry.DisplayName ||
                permission.Description != entry.Description ||
                permission.Module != entry.Module;

            if (differs)
            {
                permission.Describe(entry.DisplayName, entry.Description, entry.Module);
                updated.Add(entry.Key);
            }
            else
            {
                unchanged++;
            }
        }

        var catalogued = PermissionCatalog.All
            .Select(entry => entry.Key)
            .ToHashSet(StringComparer.Ordinal);

        var orphaned = existing
            .Select(permission => permission.Key)
            .Where(key => !catalogued.Contains(key))
            .ToList();

        return new PermissionSeedResult(created, updated, orphaned, unchanged);
    }

    private async Task<BranchSeedResult> SeedBranchesAsync(
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        // IgnoreQueryFilters: Branches filters out RemovedAt != null, and the unique index on
        // Code is filtered the same way — so a soft-removed branch is invisible to a normal
        // query and would be re-created as a duplicate the index happily permits.
        var existing = await context.Branches
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        // Two collections, not ToDictionary(b => b.Code): that same filtered index permits one
        // removed row and one live row sharing a code, and ToDictionary throws on the duplicate.
        var liveByCode = existing
            .Where(branch => !branch.IsRemoved)
            .ToDictionary(branch => branch.Code, StringComparer.Ordinal);

        var removedCodes = existing
            .Where(branch => branch.IsRemoved)
            .Select(branch => branch.Code)
            .ToHashSet(StringComparer.Ordinal);

        var created = new List<string>();
        var updated = new List<string>();
        var skippedRemoved = new List<string>();
        var unchanged = 0;

        foreach (var record in BranchSeedData.Branches)
        {
            var code = Branch.NormalizeCode(record.Code);

            if (liveByCode.TryGetValue(code, out var branch))
            {
               
                if (branch.Name != record.Name)
                {
                    branch.Rename(record.Name, now);
                    updated.Add(code);
                }
                else
                {
                    unchanged++;
                }

                continue;
            }

            if (removedCodes.Contains(code))
            {
                skippedRemoved.Add(code);
                continue;
            }

            context.Branches.Add(Branch.Create(code, record.Name, now));
            created.Add(code);

        }

            var seedCodes = BranchSeedData.Branches
                .Select(record => Branch.NormalizeCode(record.Code))
                .ToHashSet(StringComparer.Ordinal);

        
            var orphaned = existing
                .Where(branch => !branch.IsRemoved && !seedCodes.Contains(branch.Code))
                .Select(branch => branch.Code)
                .ToList();

        return new BranchSeedResult(created, updated, skippedRemoved, orphaned, unchanged);

    }
}
