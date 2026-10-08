using SBus.Domain.Common.Results;

namespace SBus.Domain.Drivers;

public static class DriverErrors
{
    public static Error NameRequired => Error.Validation(
        code: "DriverErrors.NameRequired",
        description: "اسم السواق مطلوب.");

    public static Error NameTooLong => Error.Validation(
        code: "DriverErrors.NameTooLong",
        description: "اسم السواق طويل جدًا.");

    public static Error PhoneRequired => Error.Validation(
        code: "DriverErrors.PhoneRequired",
        description: "رقم السواق مطلوب.");

    public static Error PhoneTooLong => Error.Validation(
        code: "DriverErrors.PhoneTooLong",
        description: "رقم السواق طويل جدًا.");
}
