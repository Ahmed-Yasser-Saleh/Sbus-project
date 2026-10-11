using FluentValidation;

using SBus.Application.Common.Validation;
using SBus.Application.Features.Bookings.Common;

namespace SBus.Application.Features.Bookings.Commands.CreateOnlineBooking;

public sealed class CreateOnlineBookingCommandValidator : BookingRequestValidator<CreateOnlineBookingCommand>
{
    public CreateOnlineBookingCommandValidator()
    {
        // Missing values are resolved from the current profile and validated again by the handler.
        TravelerProfileRules.FullName(RuleFor(x => x.PassengerName))
            .When(x => !string.IsNullOrWhiteSpace(x.PassengerName));
        TravelerProfileRules.PhoneNumber(RuleFor(x => x.PhoneNumber))
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}
