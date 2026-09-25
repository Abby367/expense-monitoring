using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SAFC.Expense.Infrastructure.Identity;

namespace SAFC.Expense.Tests;

public class CurrentUserTests
{
    [Fact]
    public void No_Http_Context_Is_Anonymous()
    {
        // Arrange
        var accessor = new HttpContextAccessor();
        var currentUser = new CurrentUser(accessor);

        // Assert
        Assert.Null(currentUser.UserId);
        Assert.False(currentUser.IsAuthenticated);
    }

    [Fact]
    public void Anonymous_Request_Has_No_User_Id()
    {
        // Arrange
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };
        var currentUser = new CurrentUser(accessor);

        // Assert
        Assert.Null(currentUser.UserId);
        Assert.False(currentUser.IsAuthenticated);
    }

    [Fact]
    public void Signed_In_Request_Exposes_The_User_Id()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        var identity = new ClaimsIdentity(
            [SafcClaims.ForUserId(id)],
            authenticationType: "Test");

        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        var currentUser = new CurrentUser(accessor);

        // Assert
        Assert.Equal(id, currentUser.UserId);
        Assert.True(currentUser.IsAuthenticated);
    }
    [Fact]
    public void Two_User_Id_Claims_Read_As_Null()
    {
        // Arrange — something attached a second identity. FindFirst would silently pick a
        // winner; exactly-one fails closed instead.
        var identity = new ClaimsIdentity(
            [
                SafcClaims.ForUserId(Guid.CreateVersion7()),
                SafcClaims.ForUserId(Guid.CreateVersion7())
            ],
            authenticationType: "Test");

        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        var currentUser = new CurrentUser(accessor);

        // Assert
        Assert.Null(currentUser.UserId);

        // Still authenticated — an ambiguous identity is not an anonymous one.
        Assert.True(currentUser.IsAuthenticated);
    }

    [Fact]
    public void An_Empty_Guid_Reads_As_Null()
    {
        // Arrange
        var identity = new ClaimsIdentity(
            [SafcClaims.ForUserId(Guid.Empty)],
            authenticationType: "Test");

        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        var currentUser = new CurrentUser(accessor);

        // Assert — Guid.Empty would fail safely at the permission check, but only after being
        // written into CreatedByUserId. Reject it where it is read.
        Assert.Null(currentUser.UserId);
    }

}
