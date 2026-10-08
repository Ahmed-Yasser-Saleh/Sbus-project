using FluentValidation;

using SBus.Domain.Bookings;

namespace SBus.Application.Features.Bookings.Commands.RejectPayment;

public sealed class RejectPaymentCommandValidator : AbstractValidator<RejectPaymentCommand>
{
    public RejectPaymentCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("اكتب سبب الرفض عشان يظهر للراكب.")
            .MaximumLength(Booking.RejectionReasonMaxLength);
    }
}
