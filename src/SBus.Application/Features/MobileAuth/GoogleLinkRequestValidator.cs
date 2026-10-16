using FluentValidation;

using SBus.Application.Common.Validation;

namespace SBus.Application.Features.MobileAuth;

public sealed class GoogleLinkRequestValidator : AbstractValidator<GoogleLinkRequest>
{
    public GoogleLinkRequestValidator()
    {
        RuleFor(x => x.LinkingProof).NotEmpty().MaximumLength(32768);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(AccountPasswordPolicy.MaxLength);
    }
}