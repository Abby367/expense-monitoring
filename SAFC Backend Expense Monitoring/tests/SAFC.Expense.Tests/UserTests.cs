using SAFC.Expense.Domain.Entities;
using SAFC.Expense.Domain.Enums;

namespace SAFC.Expense.Tests;

public class UserTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // Entra emits a lowercase Guid; the uppercase variant is used deliberately below.
    private const string Oid = "2f1a9c4e-8b7d-4a3f-9c21-5e6d7f8a9b0c";

    private static User Invited() =>
        User.CreateByAdminWithMicrosoft("test@example.com", "Test User", null, Now);

    [Fact]
    public void First_Sign_In_Links_Stamps_And_Activates()
    {
        var user = Invited();

        var changed = user.RecordMicrosoftSignIn(Oid, Now);

        Assert.True(changed);
        Assert.Equal(Oid, user.MicrosoftId);
        Assert.Equal(Now, user.FirstLoggedInAt);
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Equal(Now, user.UpdatedAt);
    }

    [Fact]
    public void Second_Sign_In_Changes_Nothing_And_Does_Not_Touch()
    {
        var user = Invited();
        user.RecordMicrosoftSignIn(Oid, Now);

        var later = Now.AddDays(3);
        var changed = user.RecordMicrosoftSignIn(Oid, later);

        Assert.False(changed);
        Assert.Equal(Now, user.FirstLoggedInAt);

        // UpdatedAt must still be the FIRST sign-in. Touch is guarded by `changed`; unguarded,
        // it would bump UpdatedAt on every request forever and destroy the only signal that
        // says when the record was actually edited.
        Assert.Equal(Now, user.UpdatedAt);
    }

    [Fact]
    public void Signing_In_Does_Not_Activate_A_Suspended_User()
    {
        var user = Invited();
        user.Suspend(Now);

        var changed = user.RecordMicrosoftSignIn(Oid, Now);

        Assert.Equal(UserStatus.Suspended, user.Status);

        // The link and the first-login stamp still land — only the promotion is refused.
        Assert.True(changed);
        Assert.Equal(Oid, user.MicrosoftId);
    }

    [Fact]
    public void The_Microsoft_Id_Is_Matched_Case_Insensitively()
    {
        var user = Invited();
        user.RecordMicrosoftSignIn(Oid.ToUpperInvariant(), Now);

        var changed = user.RecordMicrosoftSignIn(Oid, Now.AddDays(1));

        Assert.False(changed);
        Assert.Equal(Oid, user.MicrosoftId);
    }

    [Fact]
    public void A_Different_Microsoft_Id_Throws()
    {
        var user = Invited();
        user.RecordMicrosoftSignIn(Oid, Now);

        Assert.Throws<InvalidOperationException>(
            () => user.RecordMicrosoftSignIn(Guid.NewGuid().ToString(), Now));
    }

    [Fact]
    public void A_Removed_User_Cannot_Sign_In()
    {
        var user = Invited();
        user.Remove(Guid.NewGuid(), "Left the company.", Now);

        Assert.Throws<InvalidOperationException>(
            () => user.RecordMicrosoftSignIn(Oid, Now));
    }
    [Fact]
    public void Remove_Keeps_Status()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var adminId = Guid.NewGuid();
        var user = User.CreateByAdminWithPassword(
            "test@example.com", "Test User", "hash", adminId, now);
        user.ActivateByAdmin(now);

        // Act
        user.Remove(adminId, "some reason", now);

        // Assert
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.True(user.IsRemoved);
    }


}
