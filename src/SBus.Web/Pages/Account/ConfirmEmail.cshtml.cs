using System.Text;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;

using SBus.Application.Features.Accounts;
using SBus.Infrastructure.Identity;
using SBus.Web.Infrastructure;

namespace SBus.Web.Pages.Account;

[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Account)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ConfirmEmailModel(IAccountRecovery recovery) : PageModel
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

        if (await recovery.ConfirmAsync(UserId, Code, HttpContext.RequestAborted))
        {
            Message = "Email confirmed. You may now sign in.";
        }

        return Page();
    }
}