using FluentValidation;

using SBus.Domain.Common.Constants;

namespace SBus.Application.Features.Buses.Commands.UpdateBus;

public sealed class UpdateBusCommandValidator : AbstractValidator<UpdateBusCommand>
{
    public UpdateBusCommandValidator()
    {
        RuleFor(x => x.BusId).NotEmpty();

        RuleFor(x => x.PlateNumber)
            .NotEmpty().WithMessage("رقم اللوحة مطلوب.")
            .MaximumLength(SBusConstants.NameMaxLength);

        RuleFor(x => x.SeatLayoutId)
            .NotEmpty().WithMessage("اختار شكل الكراسي.");
    }
}
