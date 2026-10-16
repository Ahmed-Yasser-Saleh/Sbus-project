using SBus.Application.Features.MobileAuth;
using SBus.Web.Services;

namespace SBus.Web.Api.Contracts;

public static class SessionMapper
{
    public static SessionResponse Map(MobileTokens tokens)
    {
        var roles = tokens.Account.Roles.ToList();
        var destination = AccountLanding.Path(roles);
        var requiresProfile = roles.Contains(SBus.Infrastructure.Identity.Roles.Traveler)
            && destination == "/" && !tokens.Account.ProfileComplete;
        return new(tokens.AccessToken, tokens.RefreshToken, tokens.AccessExpiresAtUtc, tokens.RefreshExpiresAtUtc,
            tokens.Account.Id, tokens.Account.Roles, destination, requiresProfile);
    }

    public static GoogleResponse Map(MobileGoogleResult result) => new(result.State.ToString(), result.Tokens is null ? null : Map(result.Tokens), result.LinkingProof);
}