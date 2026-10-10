using System.ComponentModel.DataAnnotations;

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
public class ForgotPasswordModel(UserManager<AppUser> users, IAccountEmail email, ILogger<ForgotPasswordModel> logger) : PageModel
{
    [BindProperty]
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await users.FindByEmailAsync(Email.Trim());
        if (user is not null && user.EmailConfirmed && await users.HasPasswordAsync(user))
        {
            try
            {
                await email.SendResetAsync(user, ct);
            }
            catch (Exception ex) when (ex is System.Net.Mail.SmtpException or InvalidOperationException)
            {
                logger.LogError("Account email delivery failed ({FailureType}).", ex.GetType().Name);
            }
        }

        return RedirectToPage("/Account/CheckEmail");
    }
}

