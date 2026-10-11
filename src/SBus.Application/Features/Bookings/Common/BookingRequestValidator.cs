using FluentValidation;

using SBus.Domain.Common.Constants;

namespace SBus.Application.Features.Bookings.Common;

public abstract class BookingRequestValidator<TRequest> : AbstractValidator<TRequest>
    where TRequest : IBookingRequest
{
    protected BookingRequestValidator()
    {
        RuleFor(x => x.TripId).NotEmpty().WithMessage("اختار الرحلة.");

        RuleFor(x => x.PickupStopId).NotEmpty().WithMessage("اختار مكان الركوب.");

        RuleFor(x => x.DropoffStopId)
            .NotEmpty().WithMessage("اختار مكان النزول.")
            .NotEqual(x => x.PickupStopId).WithMessage("مكان الركوب ومكان النزول لازم يكونوا مختلفين.");

        RuleFor(x => x.SeatNumbers)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(s => s.Count > 0).WithMessage("اختار كرسي واحد على الأقل.")
            .Must(s => s.Count <= SBusConstants.MaxSeatsPerBooking)
            .WithMessage("أقصى عدد كراسي في الحجز الواحد {0}.")
            .WithState(_ => new object[] { SBusConstants.MaxSeatsPerBooking })
            .Must(s => s.Distinct().Count() == s.Count).WithMessage("نفس الكرسي متكرر.");
    }
}
