
using SAFC.Expense.Domain.Entities;

namespace SAFC.Expense.Tests;

public class PermissionTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Lowercases_Key()
    {
       var permission = Permission.Create("  Users.Create  ", "desc", "Users");
        Assert.Equal("users.create", permission.Key);  

    }
    [Fact]
    public void Create_Rejects_Blank_Key()
    {
       Assert.Throws<ArgumentException>(() => Permission.Create("   ", "Create Expense", "Expenses"));
    }
    [Fact]
    public void Create_Rejects_Key_Over_Max_Length()
    {
        var longKey = new string('A', Permission.KeyMaxLength + 1);
        Assert.Throws<ArgumentException>(() => Permission.Create(longKey, "Create Expense", "Expenses"));
    }
}