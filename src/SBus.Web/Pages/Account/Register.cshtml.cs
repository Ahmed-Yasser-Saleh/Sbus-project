using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

using SBus.Application.Features.Accounts.Commands.RegisterTraveler;
using SBus.Web.Infrastructure;

namespace SBus.Web.Pages.Account;

[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Account)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class RegisterModel(ISender sender, IStringLocalizer<SharedResource> localizer) : PageModel
{
    [BindProperty]
    public string? Email { get; set; } = string.Empty;

    [BindProperty]
    public string? Password { get; set; } = string.Empty;

    [BindProperty]
    public string? ConfirmPassword { get; set; } = string.Empty;

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await sender.Send(new RegisterTravelerCommand(Email, Password, ConfirmPassword), ct);
        if (result.IsSuccess)
        {
            TempData[nameof(LoginModel.RegistrationCompleted)] = true;
            return RedirectToPage("/Account/Login");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Code, localizer[error.Template, [.. error.Args]]);
        }

        Password = string.Empty;
        ConfirmPassword = string.Empty;
        ModelState.SetModelValue(nameof(Password), string.Empty, string.Empty);
        ModelState.SetModelValue(nameof(ConfirmPassword), string.Empty, string.Empty);
        return Page();
    }
}

