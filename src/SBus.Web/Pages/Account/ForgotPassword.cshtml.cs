using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

using SBus.Application.Features.Accounts;
using SBus.Infrastructure.Identity;
using SBus.Web.Infrastructure;
using SBus.Web.Services;

namespace SBus.Web.Pages.Account;

[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Account)]
public class ForgotPasswordModel(IAccountRecovery recovery) : PageModel
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

        await recovery.SendResetAsync(Email, ct);

        return RedirectToPage("/Account/CheckEmail", new { purpose = "reset-password" });
    }
}