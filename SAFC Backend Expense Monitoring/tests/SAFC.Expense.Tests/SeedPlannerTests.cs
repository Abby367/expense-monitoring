using SAFC.Expense.Application.Utilities.SeedDefaults;
using SAFC.Expense.Domain.Authorization;
using SAFC.Expense.Domain.Entities;

namespace SAFC.Expense.Tests;

public class SeedPlannerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static Permission Perm(string key) =>
        Permission.Create(key, $"{key} display", $"{key} description", key.Split('.')[0]);

    private static SystemRoleDefinitions.Definition RoleDefinition(
        string code,
        bool isSystem,
        IReadOnlyList<string> keys,
        string name = "Defined Name",
        string description = "Defined description.") =>
        new(code, name, description, isSystem, keys);

    // Grants can only be built through Role: RolePermission's constructor is internal to Domain
    // and Domain grants InternalsVisibleTo to nobody. The Permission objects passed here must be
    // the same instances handed to PlanRoles as existingPermissions, or keyById cannot resolve
    // their ids back to keys and every grant looks absent.

    private static Role SystemRole(
        string code, string name, string description, params Permission[] grants)
    {
        var role = Role.CreateSystem(code, name, description, Now);
        role.SyncSystemDefinition(name, description, grants.Select(p => p.Id), null, Now);
        return role;
    }

    private static Role CustomRole(
        string code, string name, string description, params Permission[] grants)
    {
        var role = Role.Create(code, name, description, Now);
        role.SetPermissions(grants.Select(p => p.Id), null, Now);
        return role;
    }

    
    private static Role RemovedRole(string code, string name)
    {
        var role = Role.Create(code, name, "Retired.", Now);
        role.Remove(Guid.CreateVersion7(), "Retired for testing.", Now);
        return role;
    }

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


    // ---------------- PlanRoles ----------------

    [Fact]
    public void Planning_Does_Not_Mutate_Existing_Roles()
    {
        var view = Perm("users.view");
        var create = Perm("users.create");
        var superAdmin = SystemRole(RoleCodes.SuperAdmin, "Old Name", "Old description.", view);

        var updatedAtBefore = superAdmin.UpdatedAt;

        SeedPlanner.PlanRoles(
            [superAdmin], [view, create],
            [RoleDefinition(RoleCodes.SuperAdmin, true, [view.Key, create.Key])]);

        Assert.Equal("Old Name", superAdmin.Name);
        Assert.Equal("Old description.", superAdmin.Description);
        Assert.Single(superAdmin.RolePermissions);

        // Not Assert.Null, unlike the branch equivalent: building the fixture goes through
        // SyncSystemDefinition, whose Reconcile calls Touch. The claim is that planning did
        // not move it, not that it was never set.
        Assert.Equal(updatedAtBefore, superAdmin.UpdatedAt);
    }

    [Theory]
    [InlineData(true)]   // stored system, definition says it is not
    [InlineData(false)]  // stored not system, definition says it is
    public void IsSystem_Mismatch_Takes_Neither_Path(bool storedIsSystem)
    {
        var view = Perm("users.view");

        var role = storedIsSystem
            ? SystemRole(RoleCodes.Admin, "Stored Name", "Stored description.", view)
            : CustomRole(RoleCodes.Admin, "Stored Name", "Stored description.", view);

        var plan = SeedPlanner.PlanRoles(
            [role], [view],
            [RoleDefinition(RoleCodes.Admin, !storedIsSystem, [view.Key], name: "Different Name")]);

        Assert.Equal(RoleCodes.Admin, Assert.Single(plan.Conflicts));
        Assert.Empty(plan.ToSync);
        Assert.Empty(plan.ToCreate);
        Assert.Empty(plan.Unmanaged);
        Assert.Equal(0, plan.Unchanged);
    }

    [Fact]
    public void Existing_Non_System_Role_Is_Never_Synced()
    {
        var view = Perm("users.view");
        var remove = Perm("users.remove");

        // An admin unticked users.remove and renamed the role. Both must survive a seed run.
        var admin = CustomRole(RoleCodes.Admin, "Renamed By An Admin", "Edited description.", view);

        var plan = SeedPlanner.PlanRoles(
            [admin], [view, remove],
            [RoleDefinition(RoleCodes.Admin, false, [view.Key, remove.Key])]);

        Assert.Empty(plan.ToSync);
        Assert.Empty(plan.ToCreate);
        Assert.Empty(plan.Conflicts);
        Assert.Equal(1, plan.Unchanged);

        // D11 as a red build: the grant stays revoked and the name stays edited.
        Assert.Single(admin.RolePermissions);
        Assert.Equal("Renamed By An Admin", admin.Name);
    }

    [Theory]
    [InlineData("Different Name", "Defined description.")]
    [InlineData("Defined Name", "Different description.")]
    public void Drifted_System_Role_Is_Planned_For_Sync(string storedName, string storedDescription)
    {
        var view = Perm("users.view");
        var superAdmin = SystemRole(RoleCodes.SuperAdmin, storedName, storedDescription, view);

        var plan = SeedPlanner.PlanRoles(
            [superAdmin], [view],
            [RoleDefinition(RoleCodes.SuperAdmin, true, [view.Key])]);

        Assert.Same(superAdmin, Assert.Single(plan.ToSync).Existing);
        Assert.Equal(0, plan.Unchanged);
    }

    [Fact]
    public void System_Role_Holding_An_Extra_Grant_Is_Planned_For_Sync()
    {
        var view = Perm("users.view");
        var stale = Perm("expenses.approve");
        var superAdmin = SystemRole(
            RoleCodes.SuperAdmin, "Defined Name", "Defined description.", view, stale);

        var plan = SeedPlanner.PlanRoles(
            [superAdmin], [view, stale],
            [RoleDefinition(RoleCodes.SuperAdmin, true, [view.Key])]);

        Assert.Same(superAdmin, Assert.Single(plan.ToSync).Existing);
        Assert.Equal(0, plan.Unchanged);
    }

    [Fact]
    public void System_Role_Missing_A_Newly_Catalogued_Grant_Is_Planned_For_Sync()
    {
        var view = Perm("users.view");
        var added = Perm("branches.view");
        var superAdmin = SystemRole(
            RoleCodes.SuperAdmin, "Defined Name", "Defined description.", view);

        var plan = SeedPlanner.PlanRoles(
            [superAdmin], [view, added],
            [RoleDefinition(RoleCodes.SuperAdmin, true, [view.Key, added.Key])]);

        Assert.Same(superAdmin, Assert.Single(plan.ToSync).Existing);
        Assert.Equal(0, plan.Unchanged);
    }

    [Fact]
    public void Identical_System_Role_Is_Unchanged_Whatever_The_Grant_Order()
    {
        var view = Perm("users.view");
        var create = Perm("users.create");
        var superAdmin = SystemRole(
            RoleCodes.SuperAdmin, "Defined Name", "Defined description.", view, create);

        // Keys deliberately in the opposite order. SystemRoleDiffers uses SetEquals; swap it for
        // SequenceEqual and this goes red — which is the point, because that change would re-sync
        // SUPERADMIN on every run and bump UpdatedAt on every grant row forever.
        var plan = SeedPlanner.PlanRoles(
            [superAdmin], [view, create],
            [RoleDefinition(RoleCodes.SuperAdmin, true, [create.Key, view.Key])]);

        Assert.Empty(plan.ToSync);
        Assert.Equal(1, plan.Unchanged);
    }

    [Fact]
    public void Removed_Role_Code_Is_Skipped_Not_Recreated()
    {
        var plan = SeedPlanner.PlanRoles(
            [RemovedRole(RoleCodes.Admin, "Administrator")], [],
            [RoleDefinition(RoleCodes.Admin, false, [])]);

        Assert.Equal(RoleCodes.Admin, Assert.Single(plan.SkippedRemoved));
        Assert.Empty(plan.ToCreate);
        Assert.Empty(plan.ToSync);
        Assert.Empty(plan.Conflicts);

        // Load-bearing: a removed role absent from the live set must not ALSO be reported as
        // unmanaged, or every retired role becomes permanent noise in the report.
        Assert.Empty(plan.Unmanaged);
        Assert.Equal(0, plan.Unchanged);
    }

    [Fact]
    public void Live_And_Removed_Role_Sharing_A_Code_Use_The_Live_Row()
    {
        var view = Perm("users.view");
        var removed = RemovedRole(RoleCodes.Admin, "Administrator (old)");
        var live = CustomRole(RoleCodes.Admin, "Administrator", "Current.", view);

        var plan = SeedPlanner.PlanRoles(
            [removed, live], [view],
            [RoleDefinition(RoleCodes.Admin, false, [view.Key])]);

        // The live row wins the lookup, so it counts as unchanged and nothing is skipped or
        // created. Collapse the two collections into one ToDictionary and this throws instead.
        Assert.Equal(1, plan.Unchanged);
        Assert.Empty(plan.SkippedRemoved);
        Assert.Empty(plan.ToCreate);
    }

    [Fact]
    public void Missing_Role_Code_Is_Planned_For_Creation()
    {
        // Lower-case on purpose: the real definitions are already uppercase, so only a fixture
        // like this proves Role.NormalizeCode is called rather than the input being trusted.
        var plan = SeedPlanner.PlanRoles([], [], [RoleDefinition("superadmin", true, [])]);

        Assert.Equal(RoleCodes.SuperAdmin, Assert.Single(plan.ToCreate).Code);
        Assert.Empty(plan.ToSync);
        Assert.Empty(plan.Conflicts);
        Assert.Equal(0, plan.Unchanged);
    }

    [Fact]
    public void Unmanaged_Lists_Live_Roles_Not_In_The_Definitions_But_Not_Removed_Ones()
    {
        var approver = CustomRole("APPROVER", "Approver", "Created in the admin UI.");
        var retired = RemovedRole("AUDITOR", "Auditor");

        var plan = SeedPlanner.PlanRoles(
            [approver, retired], [],
            [RoleDefinition(RoleCodes.SuperAdmin, true, [])]);

        Assert.Equal("APPROVER", Assert.Single(plan.Unmanaged));
        Assert.Equal(RoleCodes.SuperAdmin, Assert.Single(plan.ToCreate).Code);
    }

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


        Assert.Equal("ZZZ", Assert.Single(plan.Orphaned));
        Assert.Equal("MKT", Assert.Single(plan.ToCreate).Code);
    }
    
    // ---------------- PlanGrant ----------------

    private const string GranteeEmail = "admin@safc.com.ph";

    private static User Grantee() =>
        User.CreateByAdminWithMicrosoft(GranteeEmail, "Admin", null, Now);



    [Fact]
    public void Unavailable_SuperAdmin_Role_Is_Never_Granted()
    {
        var user = Grantee();
        var superAdmin = SystemRole(RoleCodes.SuperAdmin, "Super Admin", "Everything.");

        var plan = SeedPlanner.PlanGrant(
            GranteeEmail, user, superAdmin, superAdminUnavailable: true);

        Assert.Equal(GrantOutcome.RoleUnavailable, plan.Outcome);
        Assert.Null(plan.ToCreate);
        Assert.Empty(user.UserRoles);
    }

    [Fact]
    public void Live_Org_Wide_Grant_Is_Already_Held()
    {
        var user = Grantee();
        var superAdmin = SystemRole(RoleCodes.SuperAdmin, "Super Admin", "Everything.");

        user.Grant(superAdmin.Id, null, null, Now);

        var plan = SeedPlanner.PlanGrant(
            GranteeEmail, user, superAdmin, superAdminUnavailable: false);

        Assert.Equal(GrantOutcome.AlreadyHeld, plan.Outcome);
        Assert.Null(plan.ToCreate);
    }

    [Fact]
    public void Branch_Scoped_Grant_Does_Not_Count_As_Already_Held()
    {
        var user = Grantee();
        var superAdmin = SystemRole(RoleCodes.SuperAdmin, "Super Admin", "Everything.");

        user.Grant(superAdmin.Id, Guid.CreateVersion7(), null, Now);

        var plan = SeedPlanner.PlanGrant(
            GranteeEmail, user, superAdmin, superAdminUnavailable: false);

        Assert.Equal(GrantOutcome.Granted, plan.Outcome);
        Assert.Same(user, plan.ToCreate!.User);
        Assert.Equal(RoleCodes.SuperAdmin, plan.ToCreate.RoleCode);
    }

    [Fact]
    public void Revoked_Grant_Does_Not_Block_Regranting()
    {
        var user = Grantee();
        var superAdmin = SystemRole(RoleCodes.SuperAdmin, "Super Admin", "Everything.");

        var granted = user.Grant(superAdmin.Id, null, null, Now);
        user.RevokeGrant(granted.Id, Guid.CreateVersion7(), "Revoked for testing.", Now);

        var plan = SeedPlanner.PlanGrant(
            GranteeEmail, user, superAdmin, superAdminUnavailable: false);

        Assert.Equal(GrantOutcome.Granted, plan.Outcome);
        Assert.Same(user, plan.ToCreate!.User);
    }

    [Fact]
    public void Planning_Does_Not_Grant()
    {
        var user = Grantee();
        var superAdmin = SystemRole(RoleCodes.SuperAdmin, "Super Admin", "Everything.");

        var plan = SeedPlanner.PlanGrant(
            GranteeEmail, user, superAdmin, superAdminUnavailable: false);

        Assert.Equal(GrantOutcome.Granted, plan.Outcome);
        Assert.Same(user, plan.ToCreate!.User);

        // UpdatedAt staying null is a legitimate assertion for a user, unlike the role
        // equivalent above: Grant does not call Touch, where Reconcile does unconditionally.
        Assert.Empty(user.UserRoles);
        Assert.Null(user.UpdatedAt);
    }

}

