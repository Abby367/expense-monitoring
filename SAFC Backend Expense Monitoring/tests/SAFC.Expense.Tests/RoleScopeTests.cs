using SAFC.Expense.Domain.Constants;

namespace SAFC.Expense.Tests;

public class RoleScopeTests
{
    [Theory]
    [InlineData(null, "*")]
    [InlineData("", "*")]
    [InlineData("   ", "*")]
    [InlineData(" makati ", "MAKATI")]
    [InlineData("MAKATI", "MAKATI")]
    public void Normalize_Canonicalises_Scope(string? input, string expected)
    {
        // Act
        var result = RoleScope.Normalize(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("*", "MAKATI", true)]
    [InlineData("MAKATI", "MAKATI", true)]
    [InlineData("makati", "MAKATI", true)]
    [InlineData("MAKATI", "CEBU", false)]
    [InlineData("MAKATI", "*", false)]
    public void Covers_Answers_Whether_A_Grant_Satisfies_A_Request(
        string granted, string requested, bool expected)
    {
        // Act
        var result = RoleScope.Covers(granted, requested);

        // Assert
        Assert.Equal(expected, result);
    }
}
