using FluentValidation;

using SBus.Application.Common.Validation;

namespace SBus.Application.Features.MobileAuth;

public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(256);
}