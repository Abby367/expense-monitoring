using SAFC.Expense.Domain.Authorization;

namespace SAFC.Expense.Tests;

public class BranchScopeTests
{
    // Static readonly fields, not [InlineData]: attribute arguments must be compile-time
    // constants and a Guid is not one. The old string-based RoleScopeTests could use a [Theory];
    // this cannot. Searchable: "attribute argument must be a constant expression".
    private static readonly Guid Makati = Guid.CreateVersion7();
    private static readonly Guid Cebu = Guid.CreateVersion7();

    [Fact]
    public void Covers_OrgWide_Covers_A_Branch()
        => Assert.True(BranchScope.OrgWide.Covers(BranchScope.At(Makati)));

    [Fact]
    public void Covers_Branch_Covers_Itself()
        => Assert.True(BranchScope.At(Makati).Covers(BranchScope.At(Makati)));

    [Fact]
    public void Covers_Branch_Does_Not_Cover_Another_Branch()
        => Assert.False(BranchScope.At(Makati).Covers(BranchScope.At(Cebu)));

    // The row people get wrong. Holding a role at one branch is not org-wide authority: editing
    // the approval matrix, or anything else asked at org-wide scope, must not be satisfied by it.
    [Fact]
    public void Covers_Branch_Does_Not_Cover_OrgWide()
        => Assert.False(BranchScope.At(Makati).Covers(BranchScope.OrgWide));

    [Fact]
    public void Covers_OrgWide_Covers_OrgWide()
        => Assert.True(BranchScope.OrgWide.Covers(BranchScope.OrgWide));

    [Fact]
    public void At_Rejects_Empty_Guid()
        => Assert.Throws<ArgumentException>(() => BranchScope.At(Guid.Empty));

    
    [Fact]
    public void Equality_Is_By_Value()
    {
        Assert.Equal(BranchScope.At(Makati), BranchScope.At(Makati));
        Assert.NotEqual(BranchScope.At(Makati), BranchScope.At(Cebu));
    }

   
    [Fact]
    public void Covers_Throws_On_Null()
        => Assert.Throws<ArgumentNullException>(() => BranchScope.OrgWide.Covers(null!));
}
