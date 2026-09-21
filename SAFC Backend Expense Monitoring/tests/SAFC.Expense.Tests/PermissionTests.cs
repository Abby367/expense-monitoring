
using SAFC.Expense.Domain.Entities;

namespace SAFC.Expense.Tests;

public class PermissionTests
{

    [Fact]
    public void Create_Lowercases_Key()
    {
       var permission = Permission.Create("  Users.Create  ", "Create User", "desc", "users");

        Assert.Equal("users.create", permission.Key);  

    }
    [Fact]
    public void Create_Rejects_Blank_Key()
    {
       Assert.Throws<ArgumentException>(() => Permission.Create("   ", "Create Expense", "Creates an expense", "expenses"));
    }
    [Fact]
    public void Create_Rejects_Key_Over_Max_Length()
    {
        var longKey = new string('A', Permission.KeyMaxLength + 1);
        Assert.Throws<ArgumentException>(() => Permission.Create(longKey, "Create Expense", "Creates an expense", "expenses"));
    }
        [Fact]
    public void Describe_Replaces_All_Three_Fields_And_Leaves_Key_Unchanged()
    {
        var permission = Permission.Create("users.create", "Create User", "Add a new user account.", "users");
        var originalId = permission.Id;

        permission.Describe("Add User", "Creates a user record.", "administration");

        Assert.Equal("Add User", permission.DisplayName);
        Assert.Equal("Creates a user record.", permission.Description);
        Assert.Equal("administration", permission.Module);

        Assert.Equal("users.create", permission.Key);
        Assert.Equal(originalId, permission.Id);
    }

    [Fact]
    public void Describe_Rejects_Blank_DisplayName()
    {
        var permission = Permission.Create("users.create", "Create User", "Add a new user account.", "users");

        Assert.Throws<ArgumentException>(
            () => permission.Describe("   ", "Add a new user account.", "users"));
    }

    [Fact]
    public void Describe_Normalizes_Module()
    {
        var permission = Permission.Create("users.create", "Create User", "Add a new user account.", "users");

        permission.Describe("Create User", "Add a new user account.", "  USERS  ");

        Assert.Equal("users", permission.Module);
    }

}