namespace SAFC.Expense.Domain.Constants;

public static class RoleScope
{
    public const string Global = "*";

    public const int MaxLength = 64;

    public static bool IsGlobal(string scope) => scope == Global;

    public static bool Covers(string grantedScope, string requestedScope) =>
        IsGlobal(grantedScope)
        || string.Equals(grantedScope, requestedScope, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string? scope) =>
        string.IsNullOrWhiteSpace(scope) ? Global : scope.Trim().ToUpperInvariant();
}
