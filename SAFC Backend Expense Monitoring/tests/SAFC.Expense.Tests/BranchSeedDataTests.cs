using SAFC.Expense.Application.Utilities.SeedDefaults;
using SAFC.Expense.Domain.Entities;

namespace SAFC.Expense.Tests;

public class BranchSeedDataTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Codes_Are_Normalized()
    {
        foreach (var record in BranchSeedData.Branches)
            Assert.Equal(Branch.NormalizeCode(record.Code), record.Code);
    }

    [Fact]
    public void Codes_Are_Unique()
    {
        var duplicates = BranchSeedData.Branches
            .GroupBy(record => record.Code)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.Empty(duplicates);
        Assert.NotEmpty(BranchSeedData.Branches);

    }

    [Fact]
    public void All_Records_Construct_A_Valid_Branch()
    {
        foreach (var record in BranchSeedData.Branches)
        {
            var branch = Branch.Create(record.Code, record.Name, Now);

            Assert.Equal(record.Code, branch.Code);
            Assert.Equal(record.Name, branch.Name);
        }
    }
    [Fact]
    public void There_Are_Exactly_Ninety_One_Branches()
    {

        Assert.Equal(91, BranchSeedData.Branches.Count);
    }

}
