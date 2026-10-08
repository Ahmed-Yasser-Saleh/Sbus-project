using SBus.Domain.Common.Constants;
using SBus.Domain.Common.Results;

namespace SBus.Domain.Bookings;

public static class BookingErrors
{
    public static Error TripRequired => Error.Validation(
        code: "BookingErrors.TripRequired",
        description: "اختار الرحلة.");

    public static Error PublicTokenRequired => Error.Validation(
        code: "BookingErrors.PublicTokenRequired",
        description: "كود الحجز مطلوب.");

    public static Error PassengerNameRequired => Error.Validation(
        code: "BookingErrors.PassengerNameRequired",
        description: "اكتب اسم الراكب.");

    public static Error PassengerNameTooLong => Error.Validation(
        code: "BookingErrors.PassengerNameTooLong",
        description: "اسم الراكب طويل جدًا.");

    public static Error PhoneRequired => Error.Validation(
        code: "BookingErrors.PhoneRequired",
        description: "اكتب رقم الموبايل.");

    public static Error PhoneTooLong => Error.Validation(
        code: "BookingErrors.PhoneTooLong",
        description: "رقم الموبايل طويل جدًا.");

    public static Error StopsRequired => Error.Validation(
        code: "BookingErrors.StopsRequired",
        description: "اختار مكان الركوب ومكان النزول.");

    public static Error SameStop => Error.Validation(
        code: "BookingErrors.SameStop",
        description: "مكان الركوب ومكان النزول لازم يكونوا مختلفين.");

    public static Error SeatsRequired => Error.Validation(
        code: "BookingErrors.SeatsRequired",
        description: "اختار كرسي واحد على الأقل.");

    public static Error TooManySeats => Error.Validation(
        code: "BookingErrors.TooManySeats",
        description: "أقصى عدد كراسي في الحجز الواحد {0}.",
        args: SBusConstants.MaxSeatsPerBooking);

    public static Error SeatNumberInvalid => Error.Validation(
        code: "BookingErrors.SeatNumberInvalid",
        description: "رقم الكرسي غير صحيح.");

    public static Error DuplicateSeats => Error.Validation(
        code: "BookingErrors.DuplicateSeats",
        description: "نفس الكرسي متكرر في الحجز.");

    public static Error PriceInvalid => Error.Validation(
        code: "BookingErrors.PriceInvalid",
        description: "سعر الكرسي غير صحيح.");

    public static Error HoldDurationInvalid => Error.Validation(
        code: "BookingErrors.HoldDurationInvalid",
        description: "مدة حجز الكرسي غير صحيحة.");

    public static Error ReceiptRequired => Error.Validation(
        code: "BookingErrors.ReceiptRequired",
        description: "ارفع صورة التحويل.");

    public static Error RejectionReasonRequired => Error.Validation(
        code: "BookingErrors.RejectionReasonRequired",
        description: "اكتب سبب الرفض عشان يظهر للراكب.");

    public static Error RejectionReasonTooLong => Error.Validation(
        code: "BookingErrors.RejectionReasonTooLong",
        description: "سبب الرفض لازم يكون أقل من {0} حرف.",
        args: Booking.RejectionReasonMaxLength);

    public static Error HoldNotOverdue => Error.Conflict(
        code: "BookingErrors.HoldNotOverdue",
        description: "مدة حجز الكرسي لسه ما خلصتش.");

    public static Error ReceiptNotAllowed(BookingStatus status)
    {
        var description = status switch
        {
            BookingStatus.AwaitingConfirmation => "صورة التحويل اترفعت قبل كده، والمكتب بيراجعها.",
            BookingStatus.Confirmed => "الحجز متأكد خلاص.",
            BookingStatus.Expired => "مدة حجز الكرسي خلصت قبل رفع التحويل. لو حولت الفلوس كلم المكتب.",
            _ => "الحجز ده مقفول ومينفعش يترفعله تحويل.",
        };

        return Error.Conflict(code: "BookingErrors.ReceiptNotAllowed", description: description);
    }

    public static Error PassengerCancellationClosed(TimeSpan cutoff) => Error.Conflict(
        code: "BookingErrors.PassengerCancellationClosed",
        description: "الإلغاء من الموقع متاح لحد {0} ساعة قبل الرحلة. كلم المكتب.",
        args: cutoff.TotalHours);

    public static Error InvalidTransition(BookingStatus current, BookingStatus next) => Error.Conflict(
        code: "BookingErrors.InvalidTransition",
        description: "مينفعش الحجز يتحول من '{0}' لـ '{1}'.",
        args: [current, next]);
}
