using System.Security.Claims;

using SBus.Application.Common.Interfaces;
using SBus.Infrastructure.Identity;

namespace SBus.Web.Services;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : IUser
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public string? Id => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public bool IsTraveler
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            return principal?.Identity?.IsAuthenticated == true
                && principal.IsInRole(Roles.Traveler)
                && !principal.IsInRole(Roles.CompanyOwner)
                && !principal.IsInRole(Roles.CompanyEmployee);
        }
    }
}
