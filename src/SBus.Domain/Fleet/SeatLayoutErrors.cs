using SBus.Domain.Common.Constants;
using SBus.Domain.Common.Results;

namespace SBus.Domain.Fleet;

public static class SeatLayoutErrors
{
    public static Error NameRequired => Error.Validation(
        code: "SeatLayoutErrors.NameRequired",
        description: "اسم شكل الكراسي مطلوب.");

    public static Error NameTooLong => Error.Validation(
        code: "SeatLayoutErrors.NameTooLong",
        description: "الاسم لازم يكون أقل من {0} حرف.",
        args: SBusConstants.NameMaxLength);

    public static Error GridRequired => Error.Validation(
        code: "SeatLayoutErrors.GridRequired",
        description: "اكتب شكل الكراسي.");

    public static Error TooManyRows => Error.Validation(
        code: "SeatLayoutErrors.TooManyRows",
        description: "أقصى عدد صفوف {0}.",
        args: SeatLayout.MaxRows);

    public static Error TooManyColumns(int row) => Error.Validation(
        code: "SeatLayoutErrors.TooManyColumns",
        description: "الصف {0} فيه أكتر من {1} خانات.",
        args: [row, SeatLayout.MaxColumns]);

    public static Error TooManySeats => Error.Validation(
        code: "SeatLayoutErrors.TooManySeats",
        description: "أقصى عدد كراسي {0}.",
        args: SeatLayout.MaxSeats);

    public static Error MultipleDrivers => Error.Validation(
        code: "SeatLayoutErrors.MultipleDrivers",
        description: "مكان السواق (D) متكرر أكتر من مرة.");

    public static Error InvalidCell(string cell, int row) => Error.Validation(
        code: "SeatLayoutErrors.InvalidCell",
        description: "الخانة \"{0}\" في الصف {1} مش مفهومة. استخدم رقم الكرسي، أو _ لمكان فاضي، أو D للسواق.",
        args: [cell, row]);

    public static Error DuplicateSeat(int seatNumber) => Error.Validation(
        code: "SeatLayoutErrors.DuplicateSeat",
        description: "الكرسي رقم {0} متكرر.",
        args: seatNumber);

    public static Error NoSeats => Error.Validation(
        code: "SeatLayoutErrors.NoSeats",
        description: "لازم يكون فيه كرسي واحد على الأقل.");
}
