using SBus.Domain.Common.Results;

namespace SBus.Domain.Schedules;

public static class ScheduleErrors
{
    public static Error DirectionInvalid => Error.Validation(
        code: "ScheduleErrors.DirectionInvalid",
        description: "الاتجاه غير صحيح.");

    public static Error PriceInvalid => Error.Validation(
        code: "ScheduleErrors.PriceInvalid",
        description: "السعر لازم يكون أكبر من صفر.");

    public static Error BusRequired => Error.Validation(
        code: "ScheduleErrors.BusRequired",
        description: "اختار الأتوبيس.");

    public static Error DriverRequired => Error.Validation(
        code: "ScheduleErrors.DriverRequired",
        description: "اختار السواق.");
}
