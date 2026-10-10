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
public class LoginModel(SignInManager<AppUser> signIn, UserManager<AppUser> users) : PageModel
{
    [BindProperty]
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [DataType(DataType.Password)]
    [StringLength(128)]
    public string Password { get; set; } = string.Empty;

    public bool GoogleEnabled { get; private set; }

    [TempData]
    public bool RegistrationCompleted { get; set; }

    public bool RequiresEmailConfirmation => signIn.Options.SignIn.RequireConfirmedEmail;

    public bool EmailConfirmationRequired { get; private set; }

    public async Task<IActionResult> OnGetAsync(bool externalError = false)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(AccountLanding.Path(User));
        }

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        GoogleEnabled = (await signIn.GetExternalAuthenticationSchemesAsync()).Any(s => s.Name == "Google");
        if (externalError)
        {
            ModelState.AddModelError(string.Empty, "Unable to sign in. Please try again.");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        GoogleEnabled = (await signIn.GetExternalAuthenticationSchemesAsync()).Any(s => s.Name == "Google");
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await users.FindByEmailAsync(Email.Trim());
        if (user is not null)
        {
            var result = await signIn.PasswordSignInAsync(user, Password, isPersistent: false, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                return LocalRedirect(AccountLanding.Path(await users.GetRolesAsync(user)));
            }

            if (result.IsNotAllowed && RequiresEmailConfirmation && !user.EmailConfirmed
                && (!users.SupportsUserLockout || !await users.IsLockedOutAsync(user)))
            {
                // Identity checks confirmation before verifying the password.
                // Reveal confirmation status only after proof of the account password.
                EmailConfirmationRequired = await users.CheckPasswordAsync(user, Password);
                if (!EmailConfirmationRequired && users.SupportsUserLockout
                    && await users.GetLockoutEnabledAsync(user))
                {
                    _ = await users.AccessFailedAsync(user);
                }
            }
        }
        else
        {
            // Perform a real password-hash verification to reduce account timing differences.
            _ = users.PasswordHasher.VerifyHashedPassword(new AppUser(), DummyHash, Password);
        }

        if (!EmailConfirmationRequired)
        {
            ModelState.AddModelError(string.Empty, "Unable to sign in. Check your credentials or try again later.");
        }

        Password = string.Empty;
        ModelState.Remove(nameof(Password));
        return Page();
    }

    private static readonly string DummyHash = new PasswordHasher<AppUser>().HashPassword(new AppUser(), Guid.NewGuid().ToString());
}

