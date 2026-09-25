using Microsoft.EntityFrameworkCore;
using SAFC.Expense.Domain.Authorization;
using SAFC.Expense.Domain.Entities;
using SAFC.Expense.Domain.Enums;
using SAFC.Expense.Infrastructure.Identity;
using SAFC.Expense.Infrastructure.Persistence;

namespace SAFC.Expense.Tests.Integration;

[Collection("Integration")]
public sealed class UserAuthorizationProviderTests(IntegrationDatabase database)
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid Actor = Guid.CreateVersion7();

    private const string Key = "users.view";

    private sealed record Seeded(Guid UserId, Guid BranchId);

    /// <summary>
    /// Seeds one user holding one role, which grants one permission, at one branch.
    /// Every flag switches off exactly one line of the resolver's query.
    /// </summary>
    private static async Task<Seeded> SeedAsync(
        ExpenseDbContext context,
        UserStatus status = UserStatus.Active,
        bool removeUser = false,
        bool removeRole = false,
        bool removeBranch = false,
        bool revokeGrant = false,
        bool orgWide = false)
    {
        var permission = Permission.Create(Key, "View Users", "See users.", "users");
        context.Permissions.Add(permission);

        // Role.Create, not CreateSystem: Role.Remove refuses system roles.
        var role = Role.Create("TESTROLE", "Test Role", "For integration tests.", Now);
        role.SetPermissions([permission.Id], null, Now);
        context.Roles.Add(role);

        var branch = Branch.Create("TST", "Test Branch", Now);
        context.Branches.Add(branch);

        var user = User.CreateByAdminWithMicrosoft("probe@safc.com.ph", "Probe", Actor, Now);

        if (status is UserStatus.Active or UserStatus.Suspended)
            user.ActivateByAdmin(Now);

        if (status is UserStatus.Suspended)
            user.Suspend(Now);

        var grant = user.Grant(role.Id, orgWide ? null : branch.Id, Actor, Now);

        if (revokeGrant)
            user.RevokeGrant(grant.Id, Actor, "Revoked for the test.", Now);

        if (removeUser)
            user.Remove(Actor, "Removed for the test.", Now);

        if (removeRole)
            role.Remove(Actor, "Removed for the test.", Now);

        if (removeBranch)
            branch.Remove(Actor, "Removed for the test.", Now);

        context.Users.Add(user);

        await context.SaveChangesAsync();

        return new Seeded(user.Id, branch.Id);
    }

    private async Task<(AuthorizationSnapshotResult Result, Guid BranchId)> ResolveAsync(
        Func<ExpenseDbContext, Task<Seeded>> seed)
    {
        await using var context = database.CreateContext();

        // Transaction per test, never committed: the resolver only reads, so rollback is all the
        // isolation needed and it costs no extra package.
        await using var transaction = await context.Database.BeginTransactionAsync();

        var seeded = await seed(context);

        var provider = new UserAuthorizationProvider(context);
        var snapshot = await provider.GetSnapshotAsync(seeded.UserId, CancellationToken.None);

        return (new AuthorizationSnapshotResult(snapshot), seeded.BranchId);
    }

    private sealed record AuthorizationSnapshotResult(
        Application.Common.Authorization.AuthorizationSnapshot Snapshot);

    // ---------- the positive control ----------

    [Fact]
    public async Task An_Active_User_With_A_Live_Grant_Has_It()
    {
        var (result, branchId) = await ResolveAsync(c => SeedAsync(c));

        Assert.False(result.Snapshot.IsEmpty);
        Assert.True(result.Snapshot.Has(Key, BranchScope.At(branchId)));
    }

    // ---------- one line of the query each ----------

    [Fact]
    public async Task A_Revoked_Grant_Grants_Nothing()
    {
        var (result, _) = await ResolveAsync(c => SeedAsync(c, revokeGrant: true));

        Assert.True(result.Snapshot.IsEmpty);
    }


    [Fact]
    public async Task A_Grant_On_A_Soft_Removed_Role_Grants_Nothing()
    {
        var (result, _) = await ResolveAsync(c => SeedAsync(c, removeRole: true));

        Assert.True(result.Snapshot.IsEmpty);
    }


    [Fact]
    public async Task A_Grant_Scoped_To_A_Soft_Removed_Branch_Grants_Nothing()
    {
        var (result, _) = await ResolveAsync(c => SeedAsync(c, removeBranch: true));

        Assert.True(result.Snapshot.IsEmpty);
    }


    [Fact]
    public async Task A_Soft_Removed_User_Gets_An_Empty_Snapshot()
    {
        var (result, _) = await ResolveAsync(c => SeedAsync(c, removeUser: true));

        Assert.True(result.Snapshot.IsEmpty);
    }

    [Fact]
    public async Task A_Suspended_User_Gets_An_Empty_Snapshot()
    {
        var (result, _) = await ResolveAsync(c => SeedAsync(c, status: UserStatus.Suspended));

        Assert.True(result.Snapshot.IsEmpty);
    }

    [Fact]
    public async Task An_Invited_User_Gets_An_Empty_Snapshot()
    {
        var (result, _) = await ResolveAsync(c => SeedAsync(c, status: UserStatus.Invited));

        Assert.True(result.Snapshot.IsEmpty);
    }


    [Fact]
    public async Task An_Id_That_Names_No_User_Gets_An_Empty_Snapshot()
    {
        await using var context = database.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var provider = new UserAuthorizationProvider(context);
        var snapshot = await provider.GetSnapshotAsync(Guid.CreateVersion7(), CancellationToken.None);

        Assert.True(snapshot.IsEmpty);
    }
}
