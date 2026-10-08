using SBus.Domain.Common.Results;

namespace SBus.Domain.Trips;

public static class TripErrors
{
    public static Error ScheduleRequired => Error.Validation(
        code: "TripErrors.ScheduleRequired",
        description: "الرحلة لازم تكون تابعة لميعاد.");

    public static Error DirectionInvalid => Error.Validation(
        code: "TripErrors.DirectionInvalid",
        description: "الاتجاه غير صحيح.");

    public static Error PriceInvalid => Error.Validation(
        code: "TripErrors.PriceInvalid",
        description: "السعر لازم يكون أكبر من صفر.");

    public static Error BusRequired => Error.Validation(
        code: "TripErrors.BusRequired",
        description: "اختار الأتوبيس.");

    public static Error DriverRequired => Error.Validation(
        code: "TripErrors.DriverRequired",
        description: "اختار السواق.");
}
