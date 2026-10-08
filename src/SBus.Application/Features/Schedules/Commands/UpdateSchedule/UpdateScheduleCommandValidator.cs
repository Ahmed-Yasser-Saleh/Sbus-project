using FluentValidation;

namespace SBus.Application.Features.Schedules.Commands.UpdateSchedule;

public sealed class UpdateScheduleCommandValidator : AbstractValidator<UpdateScheduleCommand>
{
    public UpdateScheduleCommandValidator()
    {
        RuleFor(x => x.ScheduleId).NotEmpty();

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("السعر لازم يكون أكبر من صفر.")
            .LessThan(100_000);

        RuleFor(x => x.BusId).NotEmpty().WithMessage("اختار الأتوبيس.");

        RuleFor(x => x.DriverId).NotEmpty().WithMessage("اختار السواق.");
    }
}
