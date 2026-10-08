using FluentValidation;

namespace SBus.Application.Features.Schedules.Commands.CreateSchedule;

public sealed class CreateScheduleCommandValidator : AbstractValidator<CreateScheduleCommand>
{
    public CreateScheduleCommandValidator()
    {
        RuleFor(x => x.Direction).IsInEnum().WithMessage("اختار الاتجاه.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("السعر لازم يكون أكبر من صفر.")
            .LessThan(100_000);

        RuleFor(x => x.BusId).NotEmpty().WithMessage("اختار الأتوبيس.");

        RuleFor(x => x.DriverId).NotEmpty().WithMessage("اختار السواق.");
    }
}
