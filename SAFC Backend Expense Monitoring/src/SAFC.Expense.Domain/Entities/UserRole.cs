using SAFC.Expense.Domain.Authorization;

namespace SAFC.Expense.Domain.Entities;

/// <summary>
/// One grant: a user holds a role, either at one branch or organisation-wide.
/// </summary>
/// <remarks>
/// No UpdatedAt, deliberately, unlike Branch/Role/User. A grant has no editable field — the role
/// and the branch ARE the grant, so changing either means revoking and granting again, which is
/// also what keeps the audit trail honest. There are exactly two states: created and removed.
/// An UpdatedAt column would need a Touch that nothing could legitimately call.
/// </remarks>
public sealed class UserRole
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }

    /// <summary>Null means organisation-wide. It must always be stated, never defaulted into.</summary>
    public Guid? BranchId { get; private set; }

    public Guid? GrantedById { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? RemovedById { get; private set; }
    public DateTimeOffset? RemovedAt { get; private set; }
    public string? RemovedReason { get; private set; }

    public bool IsRemoved => RemovedAt is not null;
    public bool IsOrgWide => BranchId is null;


    public BranchScope Scope =>
        BranchId is null ? BranchScope.OrgWide : BranchScope.At(BranchId.Value);

    public const int RemovedReasonMaxLength = 500;

    private UserRole() { }

    internal UserRole(
        Guid userId, Guid roleId, Guid? branchId, Guid? grantedByUserId, DateTimeOffset now)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("A grant needs a real user id.", nameof(userId));

        if (roleId == Guid.Empty)
            throw new ArgumentException("A grant needs a real role id.", nameof(roleId));


        if (branchId == Guid.Empty)
            throw new ArgumentException(
                "A branch-scoped grant needs a real branch id. Pass null for an org-wide grant.",
                nameof(branchId));

        UserId = userId;
        RoleId = roleId;
        BranchId = branchId;
        GrantedById = grantedByUserId;

        Id = Guid.CreateVersion7();
        CreatedAt = now;
    }

    internal void Remove(Guid removedByUserId, string reason, DateTimeOffset now)
    {
        if (IsRemoved)
            throw new InvalidOperationException("This grant has already been revoked.");

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (removedByUserId == Guid.Empty)
            throw new ArgumentException(
                "The user who revoked this grant cannot be empty.", nameof(removedByUserId));

        var trimmedReason = reason.Trim();

        if (trimmedReason.Length > RemovedReasonMaxLength)
            throw new ArgumentException(
                $"Removed reason cannot exceed {RemovedReasonMaxLength} characters.", nameof(reason));

        RemovedAt = now;
        RemovedById = removedByUserId;
        RemovedReason = trimmedReason;

        
    }
}
