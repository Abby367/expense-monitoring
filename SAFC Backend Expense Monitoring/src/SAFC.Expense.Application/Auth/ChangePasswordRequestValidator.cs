using FluentValidation;

namespace SAFC.Expense.Application.Auth;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public const int MinimumLength = 12;

    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .MaximumLength(128)
            .When(x => x.CurrentPassword is not null);

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Enter a new password.")
            .MinimumLength(MinimumLength)
                .WithMessage("Use at least " + MinimumLength + " characters.")
            .MaximumLength(128);
        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.CurrentPassword!)
                .WithMessage("The new password must be different from the current one.")
            .When(x => x.CurrentPassword is not null);

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("The passwords do not match.");
    }
}
