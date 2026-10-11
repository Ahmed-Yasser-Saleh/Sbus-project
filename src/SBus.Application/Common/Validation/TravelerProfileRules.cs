using FluentValidation;

using SBus.Domain.Common.Constants;

namespace SBus.Application.Common.Validation;

public static class TravelerProfileRules
{
    public const string PhonePattern = @"[0-9٠-٩۰-۹ \+\(\)\-]{11,30}";
    public const string NameRequired = "اكتب اسمك بالكامل.";
    public const string NameTooLong = "الاسم طويل جدًا.";
    public const string PhoneInvalid = "رقم الموبايل لازم يكون 11 رقم ويبدأ بـ 010 أو 011 أو 012 أو 015.";

    public static IRuleBuilderOptions<T, string?> FullName<T>(IRuleBuilder<T, string?> rule) =>
        rule.NotEmpty().WithMessage(NameRequired)
            .MaximumLength(SBusConstants.NameMaxLength).WithMessage(NameTooLong);

    public static IRuleBuilderOptions<T, string?> PhoneNumber<T>(IRuleBuilder<T, string?> rule) =>
        rule.Must(p => p is { Length: <= 30 } && EgyptianPhone.Normalize(p) is not null)
            .WithMessage(PhoneInvalid);

    public static string? NormalizePhone(string? phone)
    {
        var local = EgyptianPhone.Normalize(phone);
        return local is null ? null : "+20" + local[1..];
    }
}
