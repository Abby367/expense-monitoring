namespace SAFC.Expense.Domain.Authorization;


public sealed record BranchScope
{
    public Guid? BranchId { get; }

    public bool IsOrgWide => BranchId is null;

    private BranchScope(Guid? branchId) => BranchId = branchId;

   
    public static BranchScope OrgWide { get; } = new((Guid?)null);


    public static BranchScope At(Guid branchId)
    {
        if (branchId == Guid.Empty)
            throw new ArgumentException(
                "A branch-scoped grant needs a real branch id.", nameof(branchId));

        return new BranchScope(branchId);
    }


    public bool Covers(BranchScope target)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (IsOrgWide)
            return true;

        return BranchId == target.BranchId;
    }
}
