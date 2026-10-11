using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using SBus.Application.Features.Accounts.Dtos;
using SBus.Infrastructure.Identity;
using SBus.Web.Infrastructure;
using SBus.Web.Services;

namespace SBus.Web.Pages.Account;

[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Account)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ExternalLoginModel(SignInManager<AppUser> signIn, TravelerAccounts accounts,
    IAccountEmail email, ILogger<ExternalLoginModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet() => RedirectToPage("/Account/Login");

    public async Task<IActionResult> OnPostAsync()
    {
        if (!(await signIn.GetExternalAuthenticationSchemesAsync()).Any(s => s.Name == "Google"))
        {
            return NotFound();
        }

        var callback = Url.Page("/Account/ExternalLogin", "Callback", new { returnUrl = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : null })!;
        var properties = signIn.ConfigureExternalAuthenticationProperties("Google", callback);
        properties.SetParameter(GoogleChallengeProperties.PromptParameterKey, "select_account");
        return Challenge(properties, "Google");
    }

    public async Task<IActionResult> OnGetCallbackAsync(CancellationToken ct)
    {
        var info = await signIn.GetExternalLoginInfoAsync();
        if (info is null)
        {
            return await FailureAsync();
        }

        var result = await accounts.ResolveGoogleAsync(info);
        if (result.State == ExternalAccountState.PasswordProofRequired)
        {
            return RedirectToPage("/Account/LinkGoogle", new { returnUrl = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : null });
        }

        if (result.State != ExternalAccountState.Ready || result.User is null)
        {
            return await FailureAsync();
        }

        if (!result.User.EmailConfirmed)
        {
            try
            {
                await email.SendConfirmationAsync(result.User, ct);
            }
            catch (Exception ex) when (ex is System.Net.Mail.SmtpException or InvalidOperationException)
            {
                logger.LogError("Account email delivery failed ({FailureType}).", ex.GetType().Name);
            }

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            return RedirectToPage("/Account/CheckEmail");
        }

        var login = await signIn.ExternalLoginSignInAsync("Google", info.ProviderKey,
            isPersistent: false, bypassTwoFactor: false);
        if (!login.Succeeded)
        {
            return await FailureAsync();
        }

        var roles = await signIn.UserManager.GetRolesAsync(result.User);
        var destination = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : AccountLanding.Path(roles);
        if (AccountLanding.RequiresProfileCompletion(roles, new TravelerProfileDto(result.User.FullName, result.User.PhoneNumber)))
        {
            return RedirectToPage("/Account/CompleteProfile", new { returnUrl = destination });
        }

        return LocalRedirect(destination);
    }

    private async Task<IActionResult> FailureAsync()
    {
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        return RedirectToPage("/Account/Login", new { externalError = true, returnUrl = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : null });
    }
}

