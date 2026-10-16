using FluentValidation;

using SBus.Application.Common.Validation;

namespace SBus.Application.Features.MobileAuth;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(PublicEmailAddress.MaxLength).Must(PublicEmailAddress.IsValid);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(AccountPasswordPolicy.MaxLength);
    }
}