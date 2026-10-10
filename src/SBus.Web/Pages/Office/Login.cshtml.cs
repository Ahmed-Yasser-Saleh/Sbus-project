using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SBus.Web.Pages.Office;

[AllowAnonymous]
public class LoginModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Account/Login");
    public IActionResult OnPost() => RedirectToPage("/Account/Login");
}
