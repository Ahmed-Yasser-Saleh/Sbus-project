using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SBus.Infrastructure.Identity;
using SBus.Web.Infrastructure;

namespace SBus.Web.Pages.Office;

public class LoginModel(SignInManager<AppUser> signInManager, ILogger<LoginModel> logger) : PageModel
{
    private readonly SignInManager<AppUser> _signInManager = signInManager;
    private readonly ILogger<LoginModel> _logger = logger;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            this.AddError("اكتب الإيميل والباسورد.");
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(Email.Trim(), Password, isPersistent: true, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/Office");
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Office login locked out");
            this.AddError("محاولات غلط كتير. الحساب اتقفل ربع ساعة.");
            return Page();
        }

        this.AddError("الإيميل أو الباسورد غلط.");
        return Page();
    }
}
