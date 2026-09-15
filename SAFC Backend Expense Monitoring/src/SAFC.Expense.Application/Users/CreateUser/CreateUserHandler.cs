using FluentValidation;
using SAFC.Expense.Application.Common.Interfaces;
using SAFC.Expense.Domain.Entities;
using SAFC.Expense.Domain.Enums;


namespace SAFC.Expense.Application.Users.CreateUser;

public sealed class CreateUserHandler(
    IExpenseDbContext context,
    IPasswordHasher passwordHasher,
    ITemporaryPasswordGenerator passwordGenerator,
    IValidator<CreateUserCommand> validator,
    ICurrentUser currentUser)
{
    public async Task<CreateUserResponse> Handle(
        CreateUserCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var now = DateTimeOffset.UtcNow;

        string? temporaryPassword = null;
        User user;

        if (command.AuthMethod == AuthMethod.Password)
        {
            temporaryPassword = passwordGenerator.Generate();
            var passwordHash = passwordHasher.Hash(temporaryPassword);

            user = User.CreateByAdminWithPassword(
                command.Email,
                command.FullName,
                passwordHash,
                createdByUserId: currentUser.UserId,
                now);
        }
        else
        {
            user = User.CreateByAdminWithMicrosoft(
                command.Email,
                command.FullName,
                createdByUserId: currentUser.UserId,
                now);
        }

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        return new CreateUserResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.Status.ToString(),
            temporaryPassword,
            user.TemporaryPasswordExpiresAt);
    }
}
