using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using SBus.Infrastructure.Identity;
using SBus.Web.Infrastructure;
using SBus.Web.Services;

namespace SBus.Web.Pages.Account;

[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Account)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class LinkGoogleModel(SignInManager<AppUser> signIn, TravelerAccounts accounts) : PageModel
{
    [BindProperty]
    [Required]
    [DataType(DataType.Password)]
    [StringLength(128)]
    public string Password { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync() =>
        await signIn.GetExternalLoginInfoAsync() is null ? RedirectToPage("/Account/Login") : Page();

    public async Task<IActionResult> OnPostAsync()
    {
        var info = await signIn.GetExternalLoginInfoAsync();
        if (!ModelState.IsValid || info is null)
        {
            return RedirectToPage("/Account/Login", new { externalError = true });
        }

        var user = await accounts.LinkGoogleWithPasswordAsync(info, Password);
        if (user is not null)
        {
            var login = await signIn.ExternalLoginSignInAsync("Google", info.ProviderKey, false, bypassTwoFactor: false);
            if (login.Succeeded)
            {
                return LocalRedirect(AccountLanding.Path(await signIn.UserManager.GetRolesAsync(user)));
            }
        }

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        return RedirectToPage("/Account/Login", new { externalError = true });
    }
}

