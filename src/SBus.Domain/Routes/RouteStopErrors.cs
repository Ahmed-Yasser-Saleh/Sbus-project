using SBus.Domain.Common.Results;

namespace SBus.Domain.Routes;

public static class RouteStopErrors
{
    public static Error DirectionInvalid => Error.Validation(
        code: "RouteStopErrors.DirectionInvalid",
        description: "الاتجاه غير صحيح.");

    public static Error AtLeastTwoStops => Error.Validation(
        code: "RouteStopErrors.AtLeastTwoStops",
        description: "الخط لازم يكون فيه محطتين على الأقل: مكان الانطلاق ومكان الوصول.");

    public static Error StopRequired => Error.Validation(
        code: "RouteStopErrors.StopRequired",
        description: "كل سطر في الخط لازم يكون ليه محطة.");

    public static Error DuplicateStop => Error.Validation(
        code: "RouteStopErrors.DuplicateStop",
        description: "نفس المحطة متكررة في الخط.");

    public static Error FirstStopMustStartAtZero => Error.Validation(
        code: "RouteStopErrors.FirstStopMustStartAtZero",
        description: "أول محطة هي مكان الانطلاق، فوقتها لازم يكون 0 دقيقة.");

    public static Error MinutesMustIncrease => Error.Validation(
        code: "RouteStopErrors.MinutesMustIncrease",
        description: "وقت كل محطة لازم يكون أكبر من المحطة اللي قبلها.");
}
