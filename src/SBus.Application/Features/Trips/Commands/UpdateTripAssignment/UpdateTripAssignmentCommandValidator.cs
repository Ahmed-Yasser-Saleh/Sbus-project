using FluentValidation;

namespace SBus.Application.Features.Trips.Commands.UpdateTripAssignment;

public sealed class UpdateTripAssignmentCommandValidator : AbstractValidator<UpdateTripAssignmentCommand>
{
    public UpdateTripAssignmentCommandValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
        RuleFor(x => x.BusId).NotEmpty().WithMessage("اختار الأتوبيس.");
        RuleFor(x => x.DriverId).NotEmpty().WithMessage("اختار السواق.");
    }
}
