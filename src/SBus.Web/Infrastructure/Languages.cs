using System.Globalization;

using Microsoft.AspNetCore.Localization;

namespace SBus.Web.Infrastructure;

public static class Languages
{
    public const string Arabic = "ar";
    public const string English = "en";

    public const string FormattingCulture = "en-US";

    public static readonly string[] Supported = [Arabic, English];

    public static bool IsEnglish => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == English;

    public static string Current => IsEnglish ? English : Arabic;

    public static string Direction => IsEnglish ? "ltr" : "rtl";

    public static string CookieValue(string language) =>
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(FormattingCulture, language));
}
