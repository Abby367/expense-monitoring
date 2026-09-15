using SAFC.Expense.Domain.Entities;

namespace SAFC.Expense.Tests;

public class BranchTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Normalizes_Code()
    {
        // Arrange
        var code = "mkt";
        var name = "Makati Branch";

        // Act
        var branch = Branch.Create(code, name, Now);

        // Assert
        Assert.Equal("MKT", branch.Code);
    }

    [Fact]
    public void Create_Trims_Name_But_Preserves_Case()
    {
        // Arrange
        var code = "MKT";
        var name = "  Makati Branch  ";

        // Act
        var branch = Branch.Create(code, name, Now);

        // Assert
        Assert.Equal("Makati Branch", branch.Name);
    }

    [Fact]
    public void Create_Rejects_Blank_Code()
    {
        // Arrange
        var code = "   ";
        var name = "Makati Branch";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Branch.Create(code, name, Now));
    }
    [Fact]
    public void Create_Rejects_Code_Over_Max_Length()
    {
        // Arrange
        var code = new string('A', Branch.CodeMaxLength + 1);
        var name = "Makati Branch";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Branch.Create(code, name, Now));
    }
    [Fact]
    public void Remove_Sets_RemovedAt_And_Is_Removed()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var branch = Branch.Create("MKT", "Makati Branch", Now);

        // Act
        branch.Remove(adminId, "Closed",Now);

        // Assert
        Assert.True(branch.IsRemoved);
        Assert.Equal(Now, branch.RemovedAt);
        Assert.Equal(adminId, branch.RemovedById);
    }
    [Fact]
    public void Remove_Twice_Throws(){
        // Arrange
        var adminId = Guid.NewGuid();
        var branch = Branch.Create("MKT", "Makati Branch", Now);
        branch.Remove(adminId, "again", Now);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => branch.Remove(adminId, "Closed Again", Now));
    }
}