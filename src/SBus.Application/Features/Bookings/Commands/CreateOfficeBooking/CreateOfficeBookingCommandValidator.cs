using FluentValidation;

using SBus.Application.Common.Validation;
using SBus.Application.Features.Bookings.Common;
using SBus.Domain.Common.Constants;

namespace SBus.Application.Features.Bookings.Commands.CreateOfficeBooking;

public sealed class CreateOfficeBookingCommandValidator : BookingRequestValidator<CreateOfficeBookingCommand>
{
    public CreateOfficeBookingCommandValidator()
    {
        RuleFor(x => x.PassengerName)
            .NotEmpty().WithMessage("اكتب اسم الراكب.")
            .MaximumLength(SBusConstants.NameMaxLength).WithMessage("الاسم طويل جدًا.");

        RuleFor(x => x.PhoneNumber)
            .Must(p => EgyptianPhone.Normalize(p) is not null)
            .WithMessage("رقم الموبايل لازم يكون 11 رقم ويبدأ بـ 010 أو 011 أو 012 أو 015.");
    }
}
