using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SAFC.Expense.Application.Common.Interfaces;
using SAFC.Expense.Domain.Entities;


namespace SAFC.Expense.Application.Users.CreateUser;


public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator(IExpenseDbContext context)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Enter an email address.")
            .MaximumLength(User.EmailMaxLength)
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MustAsync(async (email, ct) => !await context.Users
                    .AnyAsync(u => u.Email == User.NormalizeEmail(email), ct))
                .WithMessage("An account with this email already exists.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Enter the person's full name.")
            .MaximumLength(User.FullNameMaxLength);
            
        RuleFor(x => x.AuthMethod).IsInEnum();



    }
}
