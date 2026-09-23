using SAFC.Expense.Domain.Authorization;
using SAFC.Expense.Domain.Entities;

namespace SAFC.Expense.Tests;

public class UserRoleTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly Guid ApproverRole = Guid.CreateVersion7();
    private static readonly Guid CustodianRole = Guid.CreateVersion7();
    private static readonly Guid Makati = Guid.CreateVersion7();
    private static readonly Guid Cebu = Guid.CreateVersion7();
    private static readonly Guid Admin = Guid.CreateVersion7();

    private static User NewUser() =>
        User.CreateByAdminWithMicrosoft("grantee@safc.com.ph", "Grantee", Admin, Now);

    // ---------------- Grant ----------------

    [Fact]
    public void Grant_Records_The_Scope_And_The_Actor()
    {
        var user = NewUser();

        var grant = user.Grant(ApproverRole, Makati, Admin, Now);

        Assert.Equal(user.Id, grant.UserId);
        Assert.Equal(ApproverRole, grant.RoleId);
        Assert.Equal(Makati, grant.BranchId);
        Assert.Equal(Admin, grant.GrantedById);
        Assert.Equal(Now, grant.CreatedAt);
        Assert.False(grant.IsRemoved);
        Assert.False(grant.IsOrgWide);
        Assert.Single(user.UserRoles);
    }

    [Fact]
    public void Grant_With_A_Null_Branch_Is_Org_Wide()
    {
        var user = NewUser();

        var grant = user.Grant(ApproverRole, null, Admin, Now);

        Assert.True(grant.IsOrgWide);
        Assert.Null(grant.BranchId);
    }


    [Fact]
    public void Same_Role_At_Two_Branches_Is_Two_Grants()
    {
        var user = NewUser();

        user.Grant(ApproverRole, Makati, Admin, Now);
        user.Grant(ApproverRole, Cebu, Admin, Now);

        Assert.Equal(2, user.UserRoles.Count);
    }

    [Fact]
    public void Same_Role_At_The_Same_Branch_Twice_Is_Refused()
    {
        var user = NewUser();
        user.Grant(ApproverRole, Makati, Admin, Now);

        Assert.Throws<InvalidOperationException>(
            () => user.Grant(ApproverRole, Makati, Admin, Now));
    }


    [Fact]
    public void Org_Wide_And_Branch_Scoped_Are_Different_Grants()
    {
        var user = NewUser();

        user.Grant(ApproverRole, null, Admin, Now);
        user.Grant(ApproverRole, Makati, Admin, Now);

        Assert.Equal(2, user.UserRoles.Count);
    }


    [Fact]
    public void A_Revoked_Grant_Does_Not_Block_Granting_It_Again()
    {
        var user = NewUser();
        var first = user.Grant(ApproverRole, Makati, Admin, Now);
        user.RevokeGrant(first.Id, Admin, "Moved branch.", Now);

        user.Grant(ApproverRole, Makati, Admin, Now);

        Assert.Equal(2, user.UserRoles.Count);
        Assert.Single(user.UserRoles, grant => !grant.IsRemoved);

    }

    [Fact]
    public void Grant_Rejects_An_Empty_Role_Id()
    {
        var user = NewUser();

        Assert.Throws<ArgumentException>(() => user.Grant(Guid.Empty, Makati, Admin, Now));
    }


    [Fact]
    public void Grant_Rejects_An_Empty_Branch_Id_But_Allows_Null()
    {
        var user = NewUser();

        Assert.Throws<ArgumentException>(() => user.Grant(ApproverRole, Guid.Empty, Admin, Now));

        var orgWide = user.Grant(ApproverRole, null, Admin, Now);
        Assert.True(orgWide.IsOrgWide);
    }

    [Fact]
    public void Grant_Is_Refused_On_A_Removed_User()
    {
        var user = NewUser();
        user.Remove(Admin, "Left the company.", Now);

        Assert.Throws<InvalidOperationException>(
            () => user.Grant(ApproverRole, Makati, Admin, Now));
    }

    [Fact]
    public void Scope_Projects_To_BranchScope()
    {
        var user = NewUser();

        Assert.Equal(BranchScope.At(Makati), user.Grant(ApproverRole, Makati, Admin, Now).Scope);
        Assert.Equal(BranchScope.OrgWide, user.Grant(CustodianRole, null, Admin, Now).Scope);
    }

    // ---------------- RevokeGrant ----------------

    [Fact]
    public void RevokeGrant_Records_Who_Why_And_When()
    {
        var user = NewUser();
        var grant = user.Grant(ApproverRole, Makati, Admin, Now);

        user.RevokeGrant(grant.Id, Admin, "  Moved to Cebu.  ", Now);

        Assert.True(grant.IsRemoved);
        Assert.Equal(Now, grant.RemovedAt);
        Assert.Equal(Admin, grant.RemovedById);
        Assert.Equal("Moved to Cebu.", grant.RemovedReason);
    }

    [Fact]
    public void RevokeGrant_Twice_Says_Already_Revoked()
    {
        var user = NewUser();
        var grant = user.Grant(ApproverRole, Makati, Admin, Now);
        user.RevokeGrant(grant.Id, Admin, "First.", Now);

        var error = Assert.Throws<InvalidOperationException>(
            () => user.RevokeGrant(grant.Id, Admin, "Second.", Now));

        
        Assert.Contains("already been revoked", error.Message);
    }

    [Fact]
    public void RevokeGrant_Rejects_An_Unknown_Grant_Id()
    {
        var user = NewUser();
        user.Grant(ApproverRole, Makati, Admin, Now);

        Assert.Throws<InvalidOperationException>(
            () => user.RevokeGrant(Guid.CreateVersion7(), Admin, "Nope.", Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RevokeGrant_Requires_A_Reason(string reason)
    {
        var user = NewUser();
        var grant = user.Grant(ApproverRole, Makati, Admin, Now);

        Assert.Throws<ArgumentException>(() => user.RevokeGrant(grant.Id, Admin, reason, Now));
    }

    [Fact]
    public void RevokeGrant_Requires_A_Real_Actor()
    {
        var user = NewUser();
        var grant = user.Grant(ApproverRole, Makati, Admin, Now);

        Assert.Throws<ArgumentException>(
            () => user.RevokeGrant(grant.Id, Guid.Empty, "No actor.", Now));
    }
}
