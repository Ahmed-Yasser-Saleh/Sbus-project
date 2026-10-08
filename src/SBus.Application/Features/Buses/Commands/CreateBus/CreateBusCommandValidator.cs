using FluentValidation;

using SBus.Domain.Common.Constants;

namespace SBus.Application.Features.Buses.Commands.CreateBus;

public sealed class CreateBusCommandValidator : AbstractValidator<CreateBusCommand>
{
    public CreateBusCommandValidator()
    {
        RuleFor(x => x.PlateNumber)
            .NotEmpty().WithMessage("رقم اللوحة مطلوب.")
            .MaximumLength(SBusConstants.NameMaxLength);

        RuleFor(x => x.SeatLayoutId)
            .NotEmpty().WithMessage("اختار شكل الكراسي.");
    }
}
