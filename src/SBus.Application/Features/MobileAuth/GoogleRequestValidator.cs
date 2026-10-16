using FluentValidation;

using SBus.Application.Common.Validation;

namespace SBus.Application.Features.MobileAuth;

public sealed class GoogleRequestValidator : AbstractValidator<GoogleRequest>
{
    public GoogleRequestValidator() => RuleFor(x => x.IdToken).NotEmpty().MaximumLength(16384);
}