using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SBus.Web.Infrastructure;

namespace SBus.Web.Pages;

public class LanguageModel : PageModel
{
    public IActionResult OnGet(string lang, string? returnUrl)
    {
        if (Languages.Supported.Contains(lang))
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                Languages.CookieValue(lang),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                });
        }

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }
}
