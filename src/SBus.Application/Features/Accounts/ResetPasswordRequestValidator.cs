using FluentValidation;

namespace SBus.Application.Features.Accounts;

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator() => RuleFor(x => x.ConfirmPassword).Equal(x => x.Password);
}