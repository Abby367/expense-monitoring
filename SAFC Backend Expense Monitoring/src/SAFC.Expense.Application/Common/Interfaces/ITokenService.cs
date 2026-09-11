using SAFC.Expense.Domain.Entities;

namespace SAFC.Expense.Application.Common.Interfaces;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);

public sealed record RefreshTokenPair(string RawValue, string Hash, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    AccessToken CreateAccessToken(User user, IReadOnlyCollection<string> permissionClaims);

    RefreshTokenPair CreateRefreshToken();

    string CreateSecureToken();

    string HashToken(string rawToken);
}
