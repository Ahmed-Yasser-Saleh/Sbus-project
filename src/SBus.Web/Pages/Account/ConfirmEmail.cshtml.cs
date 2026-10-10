using System.Text;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;

using SBus.Infrastructure.Identity;

using SBus.Web.Infrastructure;

namespace SBus.Web.Pages.Account;

[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Account)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ConfirmEmailModel(UserManager<AppUser> users) : PageModel
{
    [BindProperty(SupportsGet = true)]
    [System.ComponentModel.DataAnnotations.StringLength(128)]
    public string? UserId { get; set; }

    [BindProperty(SupportsGet = true)]
    [System.ComponentModel.DataAnnotations.StringLength(2048)]
    public string? Code { get; set; }

    public string? Message { get; private set; }

    // GET displays a confirmation form; scanners cannot consume the token.
    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        Message = "Unable to confirm this request.";
        if (!ModelState.IsValid || UserId is null || Code is null)
        {
            return Page();
        }

        var user = await users.FindByIdAsync(UserId);
        if (user is not null)
        {
            try
            {
                var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Code));
                if ((await users.ConfirmEmailAsync(user, token)).Succeeded)
                {
                    Message = "Email confirmed. You may now sign in.";
                }
            }
            catch (FormatException)
            {
                // Invalid tokens receive the same generic result as unknown accounts.
            }
        }

        return Page();
    }
}

