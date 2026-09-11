namespace SAFC.Expense.Application.Auth;

public sealed record ChangePasswordRequest(
    string NewPassword,
    string ConfirmPassword,
    string? CurrentPassword = null);
