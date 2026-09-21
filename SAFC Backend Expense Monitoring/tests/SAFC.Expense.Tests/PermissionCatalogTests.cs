using System.Reflection;
using SAFC.Expense.Domain.Authorization;
using SAFC.Expense.Domain.Entities;

namespace SAFC.Expense.Tests;

public class PermissionCatalogTests
{
    /// <summary>
    /// Every permission key declared in PermissionKeys, read out of assembly metadata.
    /// A key contains a '.' (module.action); a Module constant does not — that is the partition.
    /// It depends on no field name, so renaming Module breaks nothing here.
    /// GetNestedTypes is NOT recursive: a third nesting level would be silently skipped.
    /// </summary>
    private static List<string> DeclaredKeys() =>
        typeof(PermissionKeys)
            .GetNestedTypes()
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .Where(value => value.Contains('.'))
            .ToList();

    [Fact]
    public void All_Has_No_Duplicate_Keys()
    {
        var duplicates = PermissionCatalog.All
            .GroupBy(entry => entry.Key)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void All_Keys_Are_Normalized()
    {
        foreach (var entry in PermissionCatalog.All)
            Assert.Equal(Permission.NormalizeKey(entry.Key), entry.Key);
    }
    [Fact]
    public void All_Text_Is_Trimmed()
    {
        foreach (var entry in PermissionCatalog.All)
        {
            Assert.Equal(entry.DisplayName.Trim(), entry.DisplayName);
            Assert.Equal(entry.Description.Trim(), entry.Description);
            Assert.Equal(entry.Module.Trim(), entry.Module);
        }
    }

    [Fact]
    public void All_Keys_Start_With_Their_Module()
    {
        foreach (var entry in PermissionCatalog.All)
            Assert.StartsWith(entry.Module + ".", entry.Key);
    }

    [Fact]
    public void All_Entries_Construct_A_Valid_Permission()
    {
        foreach (var entry in PermissionCatalog.All)
        {
            var permission = Permission.Create(
                entry.Key, entry.DisplayName, entry.Description, entry.Module);

            Assert.Equal(entry.Key, permission.Key);
            Assert.Equal(entry.DisplayName, permission.DisplayName);
            Assert.Equal(entry.Description, permission.Description);
            Assert.Equal(entry.Module, permission.Module);
        }
    }

    [Fact]
    public void All_Covers_Every_Declared_PermissionKey()
    {
        var catalogued = PermissionCatalog.All.Select(entry => entry.Key).ToHashSet();

        var missing = DeclaredKeys()
            .Where(key => !catalogued.Contains(key))
            .ToList();

        Assert.Empty(missing);
    }
}
