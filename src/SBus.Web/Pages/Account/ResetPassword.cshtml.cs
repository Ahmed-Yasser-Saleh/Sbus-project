using System.ComponentModel.DataAnnotations;
using System.Text;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;

using SBus.Application.Common.Validation;
using SBus.Infrastructure.Identity;

using SBus.Web.Infrastructure;

namespace SBus.Web.Pages.Account;

[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Account)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ResetPasswordModel(UserManager<AppUser> users) : PageModel
{
    [BindProperty(SupportsGet = true)]
    [Required]
    [StringLength(128)]
    public string UserId { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    [Required]
    [StringLength(2048)]
    public string Code { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [StringLength(AccountPasswordPolicy.MaxLength, MinimumLength = AccountPasswordPolicy.MinLength)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    [Compare(nameof(Password))]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await users.FindByIdAsync(UserId);
        if (user is not null && user.EmailConfirmed && await users.HasPasswordAsync(user))
        {
            try
            {
                var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Code));
                var result = await users.ResetPasswordAsync(user, token, Password);
                if (result.Succeeded)
                {
                    // ResetPassword updates the security stamp; existing sessions are revoked
                    // at the next validation (configured on each authenticated request).
                    return RedirectToPage("/Account/Login");
                }
            }
            catch (FormatException)
            {
                // Invalid tokens receive the same generic result as unknown accounts.
            }
        }

        ModelState.AddModelError(string.Empty, "Unable to reset password. Request a new link and use a password meeting the requirements.");
        return Page();
    }
}

