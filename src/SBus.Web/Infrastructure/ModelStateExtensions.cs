using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

using SBus.Domain.Common.Results;

namespace SBus.Web.Infrastructure;

public static class ModelStateExtensions
{
    private const string SuccessKey = "Success";
    private const string SuccessArgsKey = "SuccessArgs";
    private const char ArgsSeparator = '\u001f';

    public static void AddErrors(this PageModel page, IEnumerable<Error> errors)
    {
        var localizer = Localizer(page);

        foreach (var error in errors)
        {
            page.ModelState.AddModelError(string.Empty, localizer[error.Template, [.. error.Args]]);
        }
    }

    public static void AddError(this PageModel page, string message)
    {
        page.ModelState.AddModelError(string.Empty, Localizer(page)[message]);
    }

    public static void SetSuccess(this PageModel page, string template, params object[] args)
    {
        page.TempData[SuccessKey] = template;
        page.TempData[SuccessArgsKey] = string.Join(ArgsSeparator, args.Select(a => Convert.ToString(a, System.Globalization.CultureInfo.InvariantCulture)));
    }

    public static string? TakeSuccess(this Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary tempData, IStringLocalizer localizer)
    {
        if (tempData[SuccessKey] is not string template)
        {
            return null;
        }

        var args = tempData[SuccessArgsKey] is string joined && joined.Length > 0
            ? joined.Split(ArgsSeparator).Cast<object>().ToArray()
            : [];

        return localizer[template, args];
    }

    private static IStringLocalizer Localizer(PageModel page) =>
        page.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>();
}
