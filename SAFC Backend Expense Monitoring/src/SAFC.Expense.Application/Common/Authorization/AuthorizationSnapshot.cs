using SAFC.Expense.Domain.Authorization;

namespace SAFC.Expense.Application.Common.Authorization;

/// <summary>What one person may do, as at the moment it was resolved.</summary>
/// <remarks>
/// Immutable and entity-free by construction: it outlives the request that built it, so it must
/// never hold a tracked entity or a DbContext — both would be disposed by the time it is read.
/// </remarks>
public sealed class AuthorizationSnapshot
{
    // Ordinal, never OrdinalIgnoreCase. Safe only because both sides are normalised at their
    // source: stored keys go through Permission.NormalizeKey, and requested keys are the
    // PermissionKeys consts, already lower-case and dotted. OrdinalIgnoreCase would forgive a
    // hand-typed "Users.Create" and start a second normalisation convention. Ordinal's failure
    // mode is deny, not allow.
    private readonly Dictionary<string, HashSet<BranchScope>> _reach;

    public Guid UserId { get; }

    /// <summary>True when this person may do nothing at all.</summary>
    public bool IsEmpty => _reach.Count == 0;

    private AuthorizationSnapshot(Guid userId, Dictionary<string, HashSet<BranchScope>> reach)
    {
        UserId = userId;
        _reach = reach;
    }

    /// <summary>A person with no reach. Every call to <see cref="Has"/> returns false.</summary>
    public static AuthorizationSnapshot Empty(Guid userId) =>
        new(userId, new Dictionary<string, HashSet<BranchScope>>(StringComparer.Ordinal));

    public static AuthorizationSnapshot FromGrants(
        Guid userId, IEnumerable<GrantedPermission> grants)
    {
        ArgumentNullException.ThrowIfNull(grants);

        var reach = new Dictionary<string, HashSet<BranchScope>>(StringComparer.Ordinal);

        foreach (var grant in grants)
        {
            // The one place a stored Guid? becomes a scope. Null is org-wide because that is what
            // the column means, not because it is a default.
            var scope = grant.BranchId is null
                ? BranchScope.OrgWide
                : BranchScope.At(grant.BranchId.Value);

            if (!reach.TryGetValue(grant.PermissionKey, out var scopes))
            {
                scopes = [];
                reach[grant.PermissionKey] = scopes;
            }

            // HashSet plus BranchScope's record equality collapses duplicates for free — two roles
            // granting users.view at Makati become one entry — which is why the query needs no
            // Distinct().
            scopes.Add(scope);
        }

        return new AuthorizationSnapshot(userId, reach);
    }

    public bool Has(string permissionKey, BranchScope requested)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionKey);
        ArgumentNullException.ThrowIfNull(requested);

        return _reach.TryGetValue(permissionKey, out var granted)
            && granted.Any(scope => scope.Covers(requested));
    }
}

public sealed record GrantedPermission(string PermissionKey, Guid? BranchId);
