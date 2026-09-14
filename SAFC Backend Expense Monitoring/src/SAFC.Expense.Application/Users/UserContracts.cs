using SAFC.Expense.Domain.Enums;

namespace SAFC.Expense.Application.Users;



public sealed record RoleGrantResponse(Guid RoleId, string RoleName);

public sealed record UserResponse(
    Guid Id,
    string Email,
    string FullName,
    string Status,
    bool MustChangePassword,
    IReadOnlyCollection<RoleGrantResponse> Roles,
    DateTimeOffset CreatedAt,
    DateTimeOffset? FirstLoggedInAt);

public sealed record UpdateUserRequest(string FullName);

public sealed record RoleGrantRequest(Guid RoleId, string? Scope = null);

public sealed record UpdateUserRolesRequest(IReadOnlyCollection<RoleGrantRequest> Grants);

public sealed record ResetUserPasswordRequest(string TemporaryPassword);

public sealed record BulkResult(int Affected);
