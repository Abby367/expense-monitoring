using SAFC.Expense.Infrastructure.Authentication;

namespace SAFC.Expense.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_Then_Verify_Returns_True()
    {
        // Arrange
        var hasher = new PasswordHasher();
        const string password = "correct horse battery staple";

        // Act
        var hash = hasher.Hash(password);

        // Assert
        Assert.True(hasher.Verify(password, hash));
    }

    [Fact]
    public void Verify_Returns_False_For_Wrong_Password()
    {
        // Arrange
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("correct horse battery staple");

        // Act
        var result = hasher.Verify("wrong password", hash);

        // Assert
        Assert.False(result);
    }
        [Fact]
    public void Hash_Produces_Different_Hashes_For_Same_Password()
    {
        // Arrange
        var hasher = new PasswordHasher();
        const string password = "correct horse battery staple";

        // Act
        var first = hasher.Hash(password);
        var second = hasher.Hash(password);

        // Assert
        Assert.NotEqual(first, second);
        Assert.True(hasher.Verify(password, first));
        Assert.True(hasher.Verify(password, second));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-bcrypt-hash")]
    public void Verify_Returns_False_For_Invalid_Hash(string? passwordHash)
    {
        // Arrange
        var hasher = new PasswordHasher();

        // Act
        var result = hasher.Verify("any password", passwordHash!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Hash_Uses_Work_Factor_12()
    {
        // Arrange
        var hasher = new PasswordHasher();

        // Act
        var hash = hasher.Hash("any password");

        // Assert
        Assert.StartsWith("$2a$12$", hash);
    }

}
