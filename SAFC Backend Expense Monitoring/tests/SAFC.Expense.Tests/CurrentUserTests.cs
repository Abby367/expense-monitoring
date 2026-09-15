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
            [new Claim(ClaimTypes.NameIdentifier, id.ToString())],
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
}
