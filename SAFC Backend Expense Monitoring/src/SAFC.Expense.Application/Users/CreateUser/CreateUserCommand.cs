using SAFC.Expense.Domain.Enums;

namespace SAFC.Expense.Application.Users.CreateUser;

public sealed record CreateUserCommand(
    string Email,
    string FullName,
    AuthMethod AuthMethod);