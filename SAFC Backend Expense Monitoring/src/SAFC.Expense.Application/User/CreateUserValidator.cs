using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SAFC.Expense.Application.Auth;
using SAFC.Expense.Application.Common.Interfaces;
using SAFC.Expense.Domain.Entities;


namespace SAFC.Expense.Application.Users;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{

    public CreateUserRequestValidator(IExpenseDbContext context)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Enter an email address.")
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MustAsync(async (email, ct) => !await context.Users
                    .AnyAsync(u => u.Email == User.NormalizeEmail(email), ct))
                .WithMessage("An account with this email already exists.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Enter the person's full name.");



    }
}
