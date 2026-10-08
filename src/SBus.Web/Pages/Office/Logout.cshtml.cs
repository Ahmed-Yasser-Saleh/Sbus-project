using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SBus.Infrastructure.Identity;

namespace SBus.Web.Pages.Office;

public class LogoutModel(SignInManager<AppUser> signInManager) : PageModel
{
    private readonly SignInManager<AppUser> _signInManager = signInManager;

    public IActionResult OnGet() => Redirect("/Office");

    public async Task<IActionResult> OnPostAsync()
    {
        await _signInManager.SignOutAsync();

        return Redirect("/Office/Login");
    }
}
