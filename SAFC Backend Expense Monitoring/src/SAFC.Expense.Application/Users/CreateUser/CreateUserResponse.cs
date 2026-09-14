namespace SAFC.Expense.Application.Users.CreateUser;

public sealed record CreateUserResponse(
        Guid Id,
        string Email,
        string FullName,
        string Status,
        // TODO: stop returning this once invite emails are sent
        string? TemporaryPassword,
        DateTimeOffset? TemporaryPasswordExpiresAt);