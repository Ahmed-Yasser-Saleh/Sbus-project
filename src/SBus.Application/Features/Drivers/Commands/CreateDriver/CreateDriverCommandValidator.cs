using FluentValidation;

using SBus.Application.Common.Validation;
using SBus.Domain.Common.Constants;

namespace SBus.Application.Features.Drivers.Commands.CreateDriver;

public sealed class CreateDriverCommandValidator : AbstractValidator<CreateDriverCommand>
{
    public CreateDriverCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم السواق مطلوب.")
            .MaximumLength(SBusConstants.NameMaxLength);

        RuleFor(x => x.PhoneNumber)
            .Must(p => EgyptianPhone.Normalize(p) is not null)
            .WithMessage("رقم الموبايل لازم يكون 11 رقم ويبدأ بـ 010 أو 011 أو 012 أو 015.");
    }
}
