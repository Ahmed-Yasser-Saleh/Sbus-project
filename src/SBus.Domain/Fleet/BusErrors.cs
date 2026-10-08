using SBus.Domain.Common.Results;

namespace SBus.Domain.Fleet;

public static class BusErrors
{
    public static Error PlateNumberRequired => Error.Validation(
        code: "BusErrors.PlateNumberRequired",
        description: "رقم اللوحة مطلوب.");

    public static Error PlateNumberTooLong => Error.Validation(
        code: "BusErrors.PlateNumberTooLong",
        description: "رقم اللوحة طويل جدًا.");

    public static Error SeatLayoutRequired => Error.Validation(
        code: "BusErrors.SeatLayoutRequired",
        description: "اختار شكل الكراسي.");
}
