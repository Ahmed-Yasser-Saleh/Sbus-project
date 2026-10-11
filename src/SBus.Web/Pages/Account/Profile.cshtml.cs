using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

using SBus.Application.Common.Interfaces;
using SBus.Application.Common.Security;
using SBus.Application.Features.Accounts.Commands.UpdateTravelerProfile;
using SBus.Web.Infrastructure;

namespace SBus.Web.Pages.Account;

[Authorize(Policy = AuthPolicies.Traveler)]
public class ProfileModel(ISender sender, ITravelerProfiles profiles, IStringLocalizer<SharedResource> localizer) : PageModel
{
    [BindProperty]
    public string? FullName { get; set; }

    [BindProperty]
    public string? PhoneNumber { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var profile = await profiles.GetCurrentAsync(ct);
        if (profile is null)
        {
            return Forbid();
        }

        FullName = profile.FullName;
        PhoneNumber = profile.PhoneNumber;
        return ProfilePage();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ProfilePage();
        }

        var result = await sender.Send(new UpdateTravelerProfileCommand(FullName, PhoneNumber), ct);
        if (result.IsError)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, localizer[error.Template, [.. error.Args]]);
            }

            return ProfilePage();
        }

        if (Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl!);
        }

        this.SetSuccess("تم حفظ بياناتك.");
        return RedirectToPage("/Account/Profile");
    }

    private IActionResult ProfilePage() =>
        PartialRequests.Prepare(HttpContext, "profile-form") ? Partial("_ProfileForm", this) : Page();
}
