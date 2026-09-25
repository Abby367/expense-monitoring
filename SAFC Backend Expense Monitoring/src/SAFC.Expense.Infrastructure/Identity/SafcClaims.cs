using System.Security.Claims;

namespace SAFC.Expense.Infrastructure.Identity;

/// <summary>
/// The one writer and the one reader of the application's user-id claim.
/// </summary>
public static class SafcClaims
{
    // Private on purpose. The string appears exactly once in the solution and cannot be named
    // anywhere else, so a writer and a reader that disagree cannot be written.
    private const string UserIdClaimType = "safc:uid";

    public static Claim ForUserId(Guid userId) => new(UserIdClaimType, userId.ToString("D"));

    public static Guid? ReadUserId(ClaimsPrincipal? principal)
    {
        if (principal is null)
            return null;

        // Exactly one, or none. Two claims of this type means something attached a second
        // identity; FindFirst would silently pick a winner. Fail closed instead.
        var claims = principal.FindAll(UserIdClaimType).ToList();

        if (claims.Count != 1)
            return null;

        // Guid.Empty would fail safely at the permission check — but only after being written
        // into CreatedByUserId. Reject it here.
        return Guid.TryParse(claims[0].Value, out var id) && id != Guid.Empty
            ? id
            : null;
    }

}
