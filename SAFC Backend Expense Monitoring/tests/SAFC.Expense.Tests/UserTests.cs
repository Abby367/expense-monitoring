using SAFC.Expense.Domain.Entities;
using SAFC.Expense.Domain.Enums;

namespace SAFC.Expense.Tests;

public class UserTests
{
    [Fact]
    public void Remove_Keeps_Status()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var adminId = Guid.NewGuid();
        var user = User.CreateByAdminWithPassword(
            "test@example.com", "Test User", "hash", adminId, now);
        user.Activate(now);

        // Act
        user.Remove(adminId, "some reason", now);

        // Assert
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.True(user.IsRemoved);
    }
}
