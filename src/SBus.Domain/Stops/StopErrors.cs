using SBus.Domain.Common.Constants;
using SBus.Domain.Common.Results;

namespace SBus.Domain.Stops;

public static class StopErrors
{
    public static Error NameRequired => Error.Validation(
        code: "StopErrors.NameRequired",
        description: "اسم المحطة مطلوب.");

    public static Error NameTooLong => Error.Validation(
        code: "StopErrors.NameTooLong",
        description: "اسم المحطة لازم يكون أقل من {0} حرف.",
        args: SBusConstants.NameMaxLength);
}
