using SAFC.Expense.Domain.Entities;

namespace SAFC.Expense.Tests;

public class RoleTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Normalizes_Code()
    {
        var role = Role.Create("  admin  ", "Administrator", "Full access", Now);

        Assert.Equal("ADMIN", role.Code);
    }

    [Fact]
    public void Create_Makes_A_Non_System_Role()
    {
        var role = Role.Create("ADMIN", "Administrator", "Full access", Now);

        Assert.False(role.IsSystem);
    }

    [Fact]
    public void CreateSystem_Marks_IsSystem()
    {
        var role = Role.CreateSystem("ADMIN", "Administrator", "Full access", Now);

        Assert.True(role.IsSystem);
    }

    [Fact]
    public void Update_On_System_Role_Throws()
    {
        var role = Role.CreateSystem("ADMIN", "Administrator", "Full access", Now);

        Assert.Throws<InvalidOperationException>(
            () => role.Update("New Name", "New Description", Now));
    }

    [Fact]
    public void SetPermissions_On_System_Role_Throws()
    {
        var role = Role.CreateSystem("ADMIN", "Administrator", "Full access", Now);

        Assert.Throws<InvalidOperationException>(
            () => role.SetPermissions([Guid.CreateVersion7()], null,Now));
    }

    [Fact]
    public void Remove_On_System_Role_Throws()
    {
        var role = Role.CreateSystem("ADMIN", "Administrator", "Full access", Now);

        Assert.Throws<InvalidOperationException>(
            () => role.Remove(Guid.CreateVersion7(), "Reason", Now));
    }

    [Fact]
    public void SetPermissions_Replaces_The_Whole_Set()
    {
        var role = Role.Create("APPROVER", "Approver", "Approves expenses", Now);
        var first = Guid.CreateVersion7();
        var second = Guid.CreateVersion7();

        role.SetPermissions([first], null, Now);
        role.SetPermissions([second], null, Now);

        Assert.Single(role.RolePermissions);
        Assert.Equal(second, role.RolePermissions.First().PermissionId);
    }

    [Fact]
    public void SetPermissions_Deduplicates()
    {
        var role = Role.Create("APPROVER", "Approver", "Approves expenses", Now);
        var permissionId = Guid.CreateVersion7();

        role.SetPermissions([permissionId, permissionId], null, Now);

        Assert.Single(role.RolePermissions);
        Assert.Equal(permissionId, role.RolePermissions.First().PermissionId);
    }
}
