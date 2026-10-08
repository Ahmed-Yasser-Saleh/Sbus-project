using SBus.Domain.Common.Results;

namespace SBus.Application.Common.Errors;

public static class ApplicationErrors
{
    public static Error StopNotFound => Error.NotFound(
        "ApplicationErrors.Stop.NotFound",
        "المحطة مش موجودة.");

    public static Error StopNameExists => Error.Conflict(
        "ApplicationErrors.Stop.NameExists",
        "فيه محطة بنفس الاسم.");

    public static Error StopsNotFoundOrInactive => Error.Validation(
        "ApplicationErrors.Route.StopsNotFoundOrInactive",
        "فيه محطة في الخط مش موجودة أو متوقفة.");

    public static Error SeatLayoutNotFound => Error.NotFound(
        "ApplicationErrors.SeatLayout.NotFound",
        "شكل الكراسي مش موجود.");

    public static Error SeatLayoutNameExists => Error.Conflict(
        "ApplicationErrors.SeatLayout.NameExists",
        "فيه شكل كراسي بنفس الاسم.");

    public static Error BusNotFound => Error.NotFound(
        "ApplicationErrors.Bus.NotFound",
        "الأتوبيس مش موجود.");

    public static Error BusInactive => Error.Conflict(
        "ApplicationErrors.Bus.Inactive",
        "الأتوبيس ده متوقف.");

    public static Error PlateNumberExists => Error.Conflict(
        "ApplicationErrors.Bus.PlateNumberExists",
        "فيه أتوبيس بنفس رقم اللوحة.");

    public static Error DriverNotFound => Error.NotFound(
        "ApplicationErrors.Driver.NotFound",
        "السواق مش موجود.");

    public static Error DriverInactive => Error.Conflict(
        "ApplicationErrors.Driver.Inactive",
        "السواق ده متوقف.");

    public static Error ScheduleNotFound => Error.NotFound(
        "ApplicationErrors.Schedule.NotFound",
        "الميعاد مش موجود.");

    public static Error ScheduleExists => Error.Conflict(
        "ApplicationErrors.Schedule.Exists",
        "فيه ميعاد بنفس الاتجاه والساعة.");

    public static Error TripNotFound => Error.NotFound(
        "ApplicationErrors.Trip.NotFound",
        "الرحلة مش موجودة.");

    public static Error TripDeparted => Error.Conflict(
        "ApplicationErrors.Trip.Departed",
        "الرحلة دي اتحركت خلاص، ومينفعش تتحجز.");

    public static Error TripNotBookable => Error.Conflict(
        "ApplicationErrors.Trip.NotBookable",
        "الرحلة دي مش متاحة للحجز أونلاين. كلم المكتب.");

    public static Error BookingNotFound => Error.NotFound(
        "ApplicationErrors.Booking.NotFound",
        "الحجز مش موجود.");

    public static Error ReceiptNotFound => Error.NotFound(
        "ApplicationErrors.Booking.ReceiptNotFound",
        "صورة التحويل مش موجودة.");

    public static Error StopsNotOnRoute => Error.Validation(
        "ApplicationErrors.Booking.StopsNotOnRoute",
        "مكان الركوب أو النزول مش على خط الرحلة دي.");

    public static Error PickupAfterDropoff => Error.Validation(
        "ApplicationErrors.Booking.PickupAfterDropoff",
        "مكان الركوب لازم ييجي قبل مكان النزول في اتجاه الرحلة.");

    public static Error SeatsNotInLayout(IEnumerable<int> seats) => Error.Validation(
        "ApplicationErrors.Booking.SeatsNotInLayout",
        "الكراسي دي مش موجودة في الأتوبيس: {0}.",
        string.Join(", ", seats));

    public static Error SeatsTaken(IEnumerable<int> seats) => Error.Conflict(
        "ApplicationErrors.Booking.SeatsTaken",
        "الكراسي دي اتحجزت لحد تاني: {0}. اختار كراسي تانية.",
        string.Join(", ", seats));

    public static Error SeatsJustTaken => Error.Conflict(
        "ApplicationErrors.Booking.SeatsJustTaken",
        "حد حجز نفس الكرسي في نفس اللحظة. اختار كرسي تاني.");

    public static Error LayoutDoesNotCoverBookedSeats(IEnumerable<int> seats) => Error.Conflict(
        "ApplicationErrors.Bus.LayoutDoesNotCoverBookedSeats",
        "شكل الكراسي الجديد مافيهوش كراسي محجوزة فعلًا: {0}. الغي الحجوزات دي أو انقلها الأول.",
        string.Join(", ", seats));
}
