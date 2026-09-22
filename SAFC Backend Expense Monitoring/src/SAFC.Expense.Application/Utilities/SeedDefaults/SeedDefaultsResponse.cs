namespace SAFC.Expense.Application.Utilities.SeedDefaults;

public sealed record SeedDefaultsResponse(
    bool Applied,                       
    PermissionSeedResult Permissions,
    BranchSeedResult Branches,
    RoleSeedResult Roles);


public sealed record PermissionSeedResult(
    IReadOnlyList<string> Created,
    IReadOnlyList<string> Updated,
    IReadOnlyList<string> Orphaned,
    int Unchanged);

public sealed record BranchSeedResult(
    IReadOnlyList<string> Created,
    IReadOnlyList<string> Updated,
    IReadOnlyList<string> SkippedRemoved,
    IReadOnlyList<string> Orphaned,     
    int Unchanged);

public sealed record RoleSeedResult(
    IReadOnlyList<string> Created,
    IReadOnlyList<string> Updated,
    IReadOnlyList<string> SkippedRemoved,
    IReadOnlyList<string> Unmanaged,
    IReadOnlyList<string> Conflicts,
    int Unchanged);

