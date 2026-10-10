using System.Security.Claims;

using SBus.Infrastructure.Identity;

namespace SBus.Web.Services;

public static class AccountLanding
{
    public static string Path(ClaimsPrincipal principal)
    {
        if (principal.IsInRole(Roles.CompanyOwner) || principal.IsInRole(Roles.CompanyEmployee))
        {
            return "/Account/ComingSoon";
        }

        return principal.IsInRole(Roles.Office) ? "/Office" : "/";
    }

    public static string Path(IList<string> roles)
    {
        if (roles.Contains(Roles.CompanyOwner) || roles.Contains(Roles.CompanyEmployee))
        {
            return "/Account/ComingSoon";
        }

        return roles.Contains(Roles.Office) ? "/Office" : "/";
    }
}

