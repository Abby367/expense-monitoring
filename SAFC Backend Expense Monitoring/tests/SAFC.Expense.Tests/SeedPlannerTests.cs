using SAFC.Expense.Application.Utilities.SeedDefaults;
using SAFC.Expense.Domain.Authorization;
using SAFC.Expense.Domain.Entities;

namespace SAFC.Expense.Tests;

public class SeedPlannerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // A hand-built catalogue entry, never PermissionCatalog.All: these tests must not go red
    // the day a permission is added. That is the whole reason the planner takes a parameter.
    private static readonly PermissionCatalog.Entry ViewUsers =
        new("users.view", "View Users", "See the list of users and their details.", "users");

    private static Permission PermissionFrom(PermissionCatalog.Entry entry) =>
        Permission.Create(entry.Key, entry.DisplayName, entry.Description, entry.Module);

    private static Branch RemovedBranch(string code, string name)
    {
        var branch = Branch.Create(code, name, Now);
        branch.Remove(Guid.CreateVersion7(), "Closed for testing.", Now);
        return branch;
    }

    private static BranchSeedData.BranchSeedRecord Seed(string code, string name) =>
        new(code, name);

    // ---------------- PlanPermissions ----------------

    [Fact]
    public void Missing_Key_Is_Planned_For_Creation()
    {
        var plan = SeedPlanner.PlanPermissions([], [ViewUsers]);

        Assert.Equal(ViewUsers, Assert.Single(plan.ToCreate));
        Assert.Empty(plan.ToUpdate);
        Assert.Empty(plan.Orphaned);
        Assert.Equal(0, plan.Unchanged);
    }

    [Theory]
    [InlineData("Different Name", "See the list of users and their details.", "users")]
    [InlineData("View Users", "A different description.", "users")]
    [InlineData("View Users", "See the list of users and their details.", "reports")]
    public void Any_Differing_Field_Is_Planned_For_Update(
        string displayName, string description, string module)
    {
        var existing = Permission.Create(ViewUsers.Key, displayName, description, module);

        var plan = SeedPlanner.PlanPermissions([existing], [ViewUsers]);

        var update = Assert.Single(plan.ToUpdate);
        Assert.Same(existing, update.Existing);
        Assert.Equal(ViewUsers, update.Desired);
        Assert.Empty(plan.ToCreate);
        Assert.Equal(0, plan.Unchanged);
    }

    [Fact]
    public void Identical_Row_Is_Unchanged()
    {
        var plan = SeedPlanner.PlanPermissions([PermissionFrom(ViewUsers)], [ViewUsers]);

        Assert.Empty(plan.ToCreate);
        Assert.Empty(plan.ToUpdate);
        Assert.Equal(1, plan.Unchanged);
    }

    [Fact]
    public void Uncatalogued_Row_Is_Orphaned_Never_Planned()
    {
        var retired = Permission.Create(
            "expenses.approve", "Approve Expenses", "Left over from an earlier catalogue.", "expenses");

        var plan = SeedPlanner.PlanPermissions([retired], [ViewUsers]);

        Assert.Equal("expenses.approve", Assert.Single(plan.Orphaned));
        Assert.Empty(plan.ToUpdate);
        Assert.Equal(0, plan.Unchanged);

        // The catalogued key is still absent, so it is created. An orphan is reported and left
        // alone — there is no delete list on the plan at all, which is D13 made structural.
        Assert.Equal(ViewUsers, Assert.Single(plan.ToCreate));
    }

    [Fact]
    public void Planning_Does_Not_Mutate_Existing_Permissions()
    {
        var stale = Permission.Create(ViewUsers.Key, "Stale Name", "Stale description.", "users");

        SeedPlanner.PlanPermissions([stale], [ViewUsers]);

        Assert.Equal("Stale Name", stale.DisplayName);
        Assert.Equal("Stale description.", stale.Description);
    }

    // ---------------- PlanBranches ----------------

    [Fact]
    public void Missing_Code_Is_Planned_For_Creation()
    {
        // Lower-case on purpose. The real seed file is already normalized, so only a fixture
        // like this proves the planner normalizes rather than trusting its input.
        var plan = SeedPlanner.PlanBranches([], [Seed("mkt", "Makati")]);

        var create = Assert.Single(plan.ToCreate);
        Assert.Equal("MKT", create.Code);
        Assert.Equal("Makati", create.Name);
        Assert.Empty(plan.ToRename);
        Assert.Empty(plan.SkippedRemoved);
        Assert.Empty(plan.Orphaned);
        Assert.Equal(0, plan.Unchanged);
    }

    [Fact]
    public void Only_Branches_Whose_Name_Differs_Are_Renamed()
    {
        var renamed = Branch.Create("MKT", "WRONG NAME", Now);
        var matching = Branch.Create("CEB", "Cebu", Now);

        var plan = SeedPlanner.PlanBranches(
            [renamed, matching],
            [Seed("MKT", "Makati"), Seed("CEB", "Cebu")]);

        var rename = Assert.Single(plan.ToRename);
        Assert.Same(renamed, rename.Existing);
        Assert.Equal("Makati", rename.DesiredName);
        Assert.Equal(1, plan.Unchanged);

        // The planner decides; it never applies. Rename() would have set both of these, and
        // a blind Rename on all 91 rows is what destroys UpdatedAt's meaning.
        Assert.Equal("WRONG NAME", renamed.Name);
        Assert.Null(renamed.UpdatedAt);
    }

    [Fact]
    public void Live_And_Removed_Sharing_A_Code_Use_The_Live_Row()
    {
        var removed = RemovedBranch("MKT", "Makati (old site)");
        var live = Branch.Create("MKT", "WRONG NAME", Now);

        var plan = SeedPlanner.PlanBranches([removed, live], [Seed("MKT", "Makati")]);

        var rename = Assert.Single(plan.ToRename);
        Assert.Same(live, rename.Existing);
        Assert.Empty(plan.SkippedRemoved);
        Assert.Empty(plan.ToCreate);
        Assert.Empty(plan.Orphaned);
    }

    [Fact]
    public void Removed_Code_Is_Skipped_Not_Recreated()
    {
        var plan = SeedPlanner.PlanBranches(
            [RemovedBranch("ROX", "Manila")],
            [Seed("ROX", "Manila")]);

        Assert.Equal("ROX", Assert.Single(plan.SkippedRemoved));
        Assert.Empty(plan.ToCreate);
        Assert.Empty(plan.ToRename);
        Assert.Empty(plan.Orphaned);
        Assert.Equal(0, plan.Unchanged);
    }

    [Fact]
    public void Live_Orphan_Is_Reported_And_Removed_Orphan_Is_Not()
    {
        var liveOrphan = Branch.Create("ZZZ", "Hand-inserted, not in the workbook", Now);
        var removedOrphan = RemovedBranch("QQQ", "Closed years ago");

        var plan = SeedPlanner.PlanBranches(
            [liveOrphan, removedOrphan],
            [Seed("MKT", "Makati")]);

        // Only the live one. A removed branch absent from the seed list was deliberately
        // retired — reporting it would be noise on every single run.
        Assert.Equal("ZZZ", Assert.Single(plan.Orphaned));
        Assert.Equal("MKT", Assert.Single(plan.ToCreate).Code);
    }
}
