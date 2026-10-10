using FluentValidation;

using SBus.Application.Common.Validation;

namespace SBus.Application.Features.Accounts.Commands.RegisterTraveler;

public sealed class RegisterTravelerCommandValidator : AbstractValidator<RegisterTravelerCommand>
{
    public const int PasswordMinLength = AccountPasswordPolicy.MinLength;
    public const int PasswordMaxLength = AccountPasswordPolicy.MaxLength;

    public RegisterTravelerCommandValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(PublicEmailAddress.MaxLength)
            .Must(PublicEmailAddress.IsValid)
            .WithMessage("Enter a valid email address, for example name@example.com.");

        // Identity remains authoritative for password complexity and custom policies.
        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(PasswordMinLength)
            .MaximumLength(PasswordMaxLength);

        RuleFor(x => x.ConfirmPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Confirm your password.")
            .Equal(x => x.Password).WithMessage("Passwords do not match.");
    }
}

