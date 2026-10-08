using FluentValidation;

using SBus.Domain.Common.Constants;

namespace SBus.Application.Features.SeatLayouts.Commands.CreateSeatLayout;

public sealed class CreateSeatLayoutCommandValidator : AbstractValidator<CreateSeatLayoutCommand>
{
    public CreateSeatLayoutCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم شكل الكراسي مطلوب.")
            .MaximumLength(SBusConstants.NameMaxLength);

        RuleFor(x => x.Grid)
            .NotEmpty().WithMessage("اكتب شكل الكراسي.")
            .MaximumLength(2000);
    }
}
