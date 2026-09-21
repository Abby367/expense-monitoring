using SAFC.Expense.Domain.Authorization;
using SAFC.Expense.Domain.Entities;

namespace SAFC.Expense.Tests;

public class SystemRoleDefinitionsTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void All_Codes_Are_Normalized()
    {
        foreach (var definition in SystemRoleDefinitions.All)
            Assert.Equal(Role.NormalizeCode(definition.Code), definition.Code);
    }

    [Fact]
    public void All_Codes_Are_Unique()
    {
        var duplicates = SystemRoleDefinitions.All
            .GroupBy(definition => definition.Code)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void All_Referenced_Permission_Keys_Are_Catalogued()
    {
        var catalogued = PermissionCatalog.All.Select(entry => entry.Key).ToHashSet();

        var missing = SystemRoleDefinitions.AllReferencedKeys
            .Where(key => !catalogued.Contains(key))
            .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void All_Definitions_Construct_A_Valid_Role()
    {
        foreach (var definition in SystemRoleDefinitions.All)
        {
            var role = definition.IsSystem
                ? Role.CreateSystem(definition.Code, definition.Name, definition.Description, Now)
                : Role.Create(definition.Code, definition.Name, definition.Description, Now);

            Assert.Equal(definition.Code, role.Code);
            Assert.Equal(definition.Name, role.Name);
            Assert.Equal(definition.IsSystem, role.IsSystem);
        }
    }

    [Fact]
    public void SuperAdmin_Is_System_And_Admin_Is_Not()
    {
        var superAdmin = SystemRoleDefinitions.All.Single(d => d.Code == RoleCodes.SuperAdmin);
        var admin      = SystemRoleDefinitions.All.Single(d => d.Code == RoleCodes.Admin);

        Assert.True(superAdmin.IsSystem);
        Assert.False(admin.IsSystem);
    }

    [Fact]
    public void Admin_Does_Not_Hold_Role_Write_Permissions()
    {
        var admin = SystemRoleDefinitions.All.Single(d => d.Code == RoleCodes.Admin);

        Assert.DoesNotContain(PermissionKeys.Roles.Create, admin.PermissionKeys);
        Assert.DoesNotContain(PermissionKeys.Roles.Update, admin.PermissionKeys);
        Assert.DoesNotContain(PermissionKeys.Roles.Remove, admin.PermissionKeys);
    }
}
